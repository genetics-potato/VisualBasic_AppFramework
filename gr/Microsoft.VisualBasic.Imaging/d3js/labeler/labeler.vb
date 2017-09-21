﻿Imports System.Drawing
Imports sys = System.Math

Namespace d3js

    Public Delegate Function CoolingSchedule(currT#, initialT#, nsweeps#) As Double

    ''' <summary>
    ''' A D3 plug-in for automatic label placement using simulated annealing that 
    ''' easily incorporates into existing D3 code, with syntax mirroring other 
    ''' D3 layouts.
    ''' </summary>
    ''' <remarks>
    ''' https:'github.com/tinker10/D3-Labeler
    ''' </remarks>
    Public Class Labeler

        Dim lab As Label()
        Dim anc As Anchor()
        Dim w As Double = 1, h As Double = 1 ' box width/height

        Dim max_move As Double = 5
        Dim max_angle As Double = 0.5
        Dim acc As Double = 0
        Dim rej As Double = 0

        ' weights
        Dim w_len As Double = 0.2 ' leader line length 
        Dim w_inter As Double = 1.0 ' leader line intersection
        Dim w_lab2 As Double = 30.0 ' label-label overlap
        Dim w_lab_anc As Double = 30.0 ' label-anchor overlap
        Dim w_orient As Double = 3.0 ' orientation bias

        Dim calcEnergy As Func(Of Integer, Label(), Anchor(), Double) =
            Function(i, labels, anchor)
                Return energy(i)
            End Function

        Dim definedCoolingSchedule As CoolingSchedule =
            AddressOf coolingSchedule

        ''' <summary>
        ''' energy function, tailored for label placement
        ''' </summary>
        ''' <param name="index%"></param>
        ''' <returns></returns>
        Private Function energy(index%) As Double
            Dim m = lab.Length,
                ener# = 0,
                dx = lab(index).X - anc(index).x,
                dy = anc(index).y - lab(index).Y,
                dist = Math.Sqrt(dx * dx + dy * dy),
                overlap = True,
                amount = 0

            ' penalty for length of leader line
            If (dist > 0) Then
                ener += dist * w_len
            End If

            ' label orientation bias
            dx /= dist
            dy /= dist

            If (dx > 0 AndAlso dy > 0) Then
                ener += 0 * w_orient
            ElseIf (dx < 0 AndAlso dy > 0) Then
                ener += 1 * w_orient
            ElseIf (dx < 0 AndAlso dy < 0) Then
                ener += 2 * w_orient
            Else
                ener += 3 * w_orient
            End If

            Dim x21 = lab(index).X,
                y21 = lab(index).Y - lab(index).height + 2.0,
                x22 = lab(index).X + lab(index).width,
                y22 = lab(index).Y + 2.0
            Dim x11, x12, y11, y12, x_overlap, y_overlap, overlap_area

            For i As Integer = 0 To m - 1
                If (i <> index) Then

                    ' penalty for intersection of leader lines
                    overlap = intersect(
                        anc(index).x, lab(index).X, anc(i).x, lab(i).X,
                        anc(index).y, lab(index).Y, anc(i).y, lab(i).Y)

                    If (overlap) Then
                        ener += w_inter
                    End If

                    ' penalty for label-label overlap
                    x11 = lab(i).X
                    y11 = lab(i).Y - lab(i).height + 2.0
                    x12 = lab(i).X + lab(i).width
                    y12 = lab(i).Y + 2.0
                    x_overlap = Math.Max(0, sys.Min(x12, x22) - Math.Max(x11, x21))
                    y_overlap = Math.Max(0, sys.Min(y12, y22) - Math.Max(y11, y21))
                    overlap_area = x_overlap * y_overlap
                    ener += (overlap_area * w_lab2)
                End If

                ' penalty for label-anchor overlap
                x11 = anc(i).x - anc(i).r
                y11 = anc(i).y - anc(i).r
                x12 = anc(i).x + anc(i).r
                y12 = anc(i).y + anc(i).r

                x_overlap = Math.Max(0, sys.Min(x12, x22) - Math.Max(x11, x21))
                y_overlap = Math.Max(0, sys.Min(y12, y22) - Math.Max(y11, y21))

                overlap_area = x_overlap * y_overlap
                ener += (overlap_area * w_lab_anc)
            Next

            Return ener
        End Function

        ''' <summary>
        ''' returns true if two lines intersect, else false
        ''' from http:'paulbourke.net/geometry/lineline2d/
        ''' </summary>
        ''' <param name="x1#"></param>
        ''' <param name="x2#"></param>
        ''' <param name="x3#"></param>
        ''' <param name="x4#"></param>
        ''' <param name="y1#"></param>
        ''' <param name="y2#"></param>
        ''' <param name="y3#"></param>
        ''' <param name="y4#"></param>
        ''' <returns></returns>
        Private Shared Function intersect(x1#, x2#, x3#, x4#, y1#, y2#, y3#, y4#) As Boolean
            Dim mua, mub As Double
            Dim denom, numera, numerb As Double

            denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1)
            numera = (x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)
            numerb = (x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)

            ' Is the intersection along the the segments 
            mua = numera / denom
            mub = numerb / denom

            If (Not (mua < 0 OrElse mua > 1 OrElse mub < 0 OrElse mub > 1)) Then
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Monte Carlo translation move
        ''' </summary>
        ''' <param name="currT#"></param>
        Private Sub mcmove(currT#)
            ' select a random label
            Dim i% = Math.Floor(Rnd() * lab.Length)

            ' save old coordinates
            Dim x_old = lab(i).X
            Dim y_old = lab(i).Y

            ' old energy
            Dim old_energy# = calcEnergy(i, lab, anc)

            ' random translation
            lab(i).X += (Rnd() - 0.5) * max_move
            lab(i).Y += (Rnd() - 0.5) * max_move

            ' hard wall boundaries
            If (lab(i).X > w) Then lab(i).X = x_old
            If (lab(i).X < 0) Then lab(i).X = x_old
            If (lab(i).Y > h) Then lab(i).Y = y_old
            If (lab(i).Y < 0) Then lab(i).Y = y_old

            ' New energy
            Dim new_energy# = calcEnergy(i, lab, anc)
            ' delta E
            Dim delta_energy = new_energy - old_energy

            If (Rnd() < Math.Exp(-delta_energy / currT)) Then
                acc += 1
            Else
                ' move back to old coordinates
                lab(i).X = x_old
                lab(i).Y = y_old
                rej += 1
            End If
        End Sub

        ''' <summary>
        ''' Monte Carlo rotation move
        ''' </summary>
        ''' <param name="currT"></param>
        Private Sub mcrotate(currT#)
            ' select a random label
            Dim i = Math.Floor(Rnd() * lab.Length)

            ' save old coordinates
            Dim x_old = lab(i).X
            Dim y_old = lab(i).Y

            ' old energy
            Dim old_energy# = calcEnergy(i, lab, anc)
            ' random angle
            Dim angle = (Rnd() - 0.5) * max_angle

            Dim s = Math.Sin(angle)
            Dim c = Math.Cos(angle)

            ' translate label (relative to anchor at origin):
            lab(i).X -= anc(i).x
            lab(i).Y -= anc(i).y

            ' rotate label
            Dim x_new = lab(i).X * c - lab(i).Y * s,
                y_new = lab(i).X * s + lab(i).Y * c

            ' translate label back
            lab(i).X = x_new + anc(i).x
            lab(i).Y = y_new + anc(i).y

            ' hard wall boundaries
            If (lab(i).X > w) Then lab(i).X = x_old
            If (lab(i).X < 0) Then lab(i).X = x_old
            If (lab(i).Y > h) Then lab(i).Y = y_old
            If (lab(i).Y < 0) Then lab(i).Y = y_old

            ' New energy
            Dim new_energy# = calcEnergy(i, lab, anc)
            ' delta E
            Dim delta_energy = new_energy - old_energy

            If (Rnd() < Math.Exp(-delta_energy / currT)) Then
                acc += 1
            Else
                ' move back to old coordinates
                lab(i).X = x_old
                lab(i).Y = y_old
                rej += 1
            End If
        End Sub

        ''' <summary>
        ''' linear cooling
        ''' </summary>
        ''' <param name="currT#"></param>
        ''' <param name="initialT#"></param>
        ''' <param name="nsweeps#"></param>
        ''' <returns></returns>
        Private Shared Function coolingSchedule(currT#, initialT#, nsweeps#) As Double
            Return (currT - (initialT / nsweeps))
        End Function

        ''' <summary>
        ''' main simulated annealing function
        ''' </summary>
        ''' <param name="nsweeps"></param>
        ''' <returns></returns>
        Public Function start(nsweeps) As Labeler
            Dim m = lab.Length,
                currT = 1.0,
                initialT = 1.0

            For i As Integer = 0 To nsweeps - 1
                For j As Integer = 0 To m - 1
                    If (Rnd() < 0.5) Then
                        Call mcmove(currT)
                    Else
                        Call mcrotate(currT)
                    End If
                Next

                currT = definedCoolingSchedule(currT, initialT, nsweeps)
            Next

            Return Me
        End Function

        ''' <summary>
        ''' users insert graph width
        ''' </summary>
        ''' <param name="x"></param>
        ''' <returns></returns>
        Public Function width(x#) As Labeler
            w = x
            Return Me
        End Function

        ''' <summary>
        ''' users insert graph height
        ''' </summary>
        ''' <param name="x"></param>
        ''' <returns></returns>
        Public Function height(x#) As Labeler
            h = x
            Return Me
        End Function

        Public Function Size(x As SizeF) As Labeler
            With x
                w = .Width
                h = .Height
            End With

            Return Me
        End Function

        ''' <summary>
        ''' users insert label positions
        ''' </summary>
        ''' <param name="x"></param>
        ''' <returns></returns>
        Public Function label(x As Label()) As Labeler
            lab = x
            Return Me
        End Function

        ''' <summary>
        ''' users insert anchor positions
        ''' </summary>
        ''' <param name="x"></param>
        ''' <returns></returns>
        Public Function anchor(x As Anchor()) As Labeler
            anc = x
            Return Me
        End Function

        ''' <summary>
        ''' user defined energy
        ''' </summary>
        ''' <param name="x"></param>
        ''' <returns></returns>
        Public Function Energy(x As Func(Of Integer, Label(), Anchor(), Double)) As Labeler
            calcEnergy = x
            Return Me
        End Function

        ''' <summary>
        ''' user defined cooling_schedule
        ''' </summary>
        ''' <param name="x"></param>
        ''' <returns></returns>
        Public Function CoolingSchedule(x As CoolingSchedule) As Labeler
            definedCoolingSchedule = x
            Return Me
        End Function
    End Class
End Namespace
