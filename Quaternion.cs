using System;

namespace rt;

public class Quaternion(double w, double x, double y, double z)
{
    public static readonly Quaternion NONE = new(0, 1, 0, 0);
    public double W { get; set; } = w;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Z { get; set; } = z;

    public Quaternion Normalize()
    {
        var a = Math.Sqrt(W * W + X * X + Y * Y + Z * Z);
        W /= a;
        X /= a;
        Y /= a;
        Z /= a;
        return this;
    }

    public static Quaternion FromAxisAngle(double aa, Vector axis)
    {
        var halfAngle = aa / 2.0;
        var s = Math.Sin(halfAngle);
        var normalizedAxis = axis.Normalize();
        return new Quaternion(
            Math.Cos(halfAngle),
            normalizedAxis.X * s,
            normalizedAxis.Y * s,
            normalizedAxis.Z * s
        );
    }
}