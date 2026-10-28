/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using System;

namespace CassetteMotionPro.Workspace
{
    // The coordinate math behind guided bike capture, kept free of UI types so it can be tested.
    public static class BikeFitGeometry
    {
        // Image points and ideal (millimetre) points are flattened as x0, y0, x1, y1, ...
        // The perspective transform is fitted exactly through four anchor points (both axle centers
        // and both tire tops). The residual is measured on the remaining points, which the fit never
        // saw, so it is a real estimate of error in millimetres rather than a figure diluted by the
        // anchors that match by construction.
        public static bool TryBuildPerspectiveTransform(double[] imagePoints, double[] idealPoints, out double[] transform, out double residualMillimeters)
        {
            transform = null;
            residualMillimeters = double.NaN;
            if (imagePoints == null || idealPoints == null || imagePoints.Length != idealPoints.Length ||
                imagePoints.Length % 2 != 0 || imagePoints.Length < 16)
                return false;

            int pointCount = imagePoints.Length / 2;

            // Use the two axle centers and the two tire-top points as four
            // stable anchors. Fitting all eight points through normal equations
            // can become ill-conditioned because six points share the axle
            // line, producing extreme values when landmarks are above it.
            int[] anchorIndices = new int[] { 0, 1, 4, 5 };
            double[,] system = new double[8, 8];
            double[] right = new double[8];
            int equation = 0;
            for (int anchor = 0; anchor < anchorIndices.Length; anchor++)
            {
                int i = anchorIndices[anchor];
                double x = imagePoints[i * 2];
                double y = imagePoints[i * 2 + 1];
                double destinationX = idealPoints[i * 2];
                double destinationY = idealPoints[i * 2 + 1];
                double[] xRow = new double[] { x, y, 1, 0, 0, 0, -destinationX * x, -destinationX * y };
                double[] yRow = new double[] { 0, 0, 0, x, y, 1, -destinationY * x, -destinationY * y };
                for (int column = 0; column < 8; column++)
                {
                    system[equation, column] = xRow[column];
                    system[equation + 1, column] = yRow[column];
                }
                right[equation] = destinationX;
                right[equation + 1] = destinationY;
                equation += 2;
            }

            double[] solution;
            if (!TrySolveLinearSystem(system, right, out solution))
                return false;

            double squaredError = 0;
            int checkedPoints = 0;
            for (int i = 0; i < pointCount; i++)
            {
                double projectedX;
                double projectedY;
                if (!TryApplyPerspectiveTransform(solution, imagePoints[i * 2], imagePoints[i * 2 + 1], out projectedX, out projectedY))
                    return false;
                if (Array.IndexOf(anchorIndices, i) >= 0)
                    continue;

                double dx = projectedX - idealPoints[i * 2];
                double dy = projectedY - idealPoints[i * 2 + 1];
                squaredError += (dx * dx) + (dy * dy);
                checkedPoints++;
            }

            if (checkedPoints == 0)
                return false;

            transform = solution;
            residualMillimeters = Math.Sqrt(squaredError / checkedPoints);
            return !double.IsNaN(residualMillimeters) && !double.IsInfinity(residualMillimeters);
        }

        public static bool TryApplyPerspectiveTransform(double[] transform, double pointX, double pointY, out double resultX, out double resultY)
        {
            resultX = 0;
            resultY = 0;
            if (transform == null || transform.Length != 8)
                return false;
            double denominator = transform[6] * pointX + transform[7] * pointY + 1.0;
            if (Math.Abs(denominator) < 0.0000001)
                return false;
            double x = (transform[0] * pointX + transform[1] * pointY + transform[2]) / denominator;
            double y = (transform[3] * pointX + transform[4] * pointY + transform[5]) / denominator;
            if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
                return false;
            resultX = x;
            resultY = y;
            return true;
        }

        // Angle of the level-reference line, folded into (-90, 90] degrees. A horizontal line has the
        // same tilt whichever end was clicked first, so click order must not rotate the picture by 180.
        public static double LevelAngleRadians(double firstX, double firstY, double secondX, double secondY)
        {
            double angle = Math.Atan2(secondY - firstY, secondX - firstX);
            while (angle > Math.PI / 2.0)
                angle -= Math.PI;
            while (angle <= -Math.PI / 2.0)
                angle += Math.PI;
            return angle;
        }

        // Rotates a point so the level-reference line becomes horizontal, with the first point as the
        // origin. When mirrorForward is set (a bike facing left), X is negated so forward is +X.
        public static void CorrectForLevel(double pointX, double pointY, double firstX, double firstY, double secondX, double secondY, bool mirrorForward, out double resultX, out double resultY)
        {
            double angle = LevelAngleRadians(firstX, firstY, secondX, secondY);
            double cos = Math.Cos(-angle);
            double sin = Math.Sin(-angle);
            double dx = pointX - firstX;
            double dy = pointY - firstY;
            double x = (dx * cos) - (dy * sin);
            double y = (dx * sin) + (dy * cos);
            resultX = mirrorForward ? -x : x;
            resultY = y;
        }

        private static bool TrySolveLinearSystem(double[,] matrix, double[] right, out double[] solution)
        {
            const int size = 8;
            solution = new double[size];
            double[,] augmented = new double[size, size + 1];
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                    augmented[row, column] = matrix[row, column];
                augmented[row, size] = right[row];
            }

            for (int pivot = 0; pivot < size; pivot++)
            {
                int bestRow = pivot;
                double bestValue = Math.Abs(augmented[pivot, pivot]);
                for (int row = pivot + 1; row < size; row++)
                {
                    double value = Math.Abs(augmented[row, pivot]);
                    if (value > bestValue)
                    {
                        bestValue = value;
                        bestRow = row;
                    }
                }

                if (bestValue < 0.0000000001)
                    return false;

                if (bestRow != pivot)
                {
                    for (int column = pivot; column <= size; column++)
                    {
                        double temporary = augmented[pivot, column];
                        augmented[pivot, column] = augmented[bestRow, column];
                        augmented[bestRow, column] = temporary;
                    }
                }

                double divisor = augmented[pivot, pivot];
                for (int column = pivot; column <= size; column++)
                    augmented[pivot, column] /= divisor;

                for (int row = 0; row < size; row++)
                {
                    if (row == pivot)
                        continue;
                    double factor = augmented[row, pivot];
                    for (int column = pivot; column <= size; column++)
                        augmented[row, column] -= factor * augmented[pivot, column];
                }
            }

            for (int row = 0; row < size; row++)
                solution[row] = augmented[row, size];
            return true;
        }
    }
}
