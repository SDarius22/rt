using System;

namespace rt
{
    public class Vector(double x, double y, double z)
    {
        public static Vector I = new Vector(1, 0, 0);
        public static Vector J = new Vector(0, 1, 0);
        public static Vector K = new Vector(0, 0, 1);
        
        public double X { get; set; } = x;
        public double Y { get; set; } = y;
        public double Z { get; set; } = z;

        public Vector() : this(0, 0, 0)
        {
        }

        public Vector(Vector v) : this(v.X, v.Y, v.Z)
        {
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static double operator *(Vector v, Vector b)
        {
            return v.X * b.X + v.Y * b.Y + v.Z * b.Z;
        }

        public static Vector operator ^(Vector a, Vector b)
        {
            return new Vector(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }

        public static Vector operator *(Vector v, double k)
        {
            return new Vector(v.X * k, v.Y * k, v.Z * k);
        }

        public static Vector operator /(Vector v, double k)
        {
            return new Vector(v.X / k, v.Y / k, v.Z / k);
        }

        public void Multiply(Vector k)
        {
            X *= k.X;
            Y *= k.Y;
            Z *= k.Z;
        }

        public void Divide(Vector k)
        {
            X /= k.X;
            Y /= k.Y;
            Z /= k.Z;
        }

        public double Length2()
        {
            return X * X + Y * Y + Z * Z;
        }

        public double Length()
        {
            return  Math.Sqrt(Length2());
        }

        public Vector Normalize()
        {
            var norm = Length();
            if (norm > 0.0)
            {
                X /= norm;
                Y /= norm;
                Z /= norm;
            }
            return this;
        }

        public void Rotate(Quaternion q)
        {
            // Normalize quaternion (without mutating the original)
            double qw = q.W, qx = q.X, qy = q.Y, qz = q.Z;
            double n = Math.Sqrt(qw * qw + qx * qx + qy * qy + qz * qz);
            if (n < 1e-15) return; // degenerate quaternion, no-op

            double inv = 1.0 / n;
            qw *= inv; qx *= inv; qy *= inv; qz *= inv;

            // t = 2 * (q.xyz × v)
            double tx = 2.0 * (qy * Z - qz * Y);
            double ty = 2.0 * (qz * X - qx * Z);
            double tz = 2.0 * (qx * Y - qy * X);

            // v' = v + q.w * t + (q.xyz × t)
            double cx = qy * tz - qz * ty;
            double cy = qz * tx - qx * tz;
            double cz = qx * ty - qy * tx;

            X += qw * tx + cx;
            Y += qw * ty + cy;
            Z += qw * tz + cz;
        }

    }
}