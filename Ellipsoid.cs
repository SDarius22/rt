using System;

namespace rt;

public class Ellipsoid : Geometry
{
    public Ellipsoid(Vector center, Vector semiAxesLength, double radius, Material material, Color color) : base(
        material, color)
    {
        Center = center;
        SemiAxesLength = semiAxesLength;
        Radius = radius;
    }

    public Ellipsoid(Vector center, Vector semiAxesLength, double radius, Color color) : base(color)
    {
        Center = center;
        SemiAxesLength = semiAxesLength;
        Radius = radius;
    }

    public Ellipsoid(Ellipsoid e) : this(new Vector(e.Center), new Vector(e.SemiAxesLength), e.Radius,
        new Material(e.Material), new Color(e.Color))
    {
    }

    private Vector Center { get; }
    private Vector SemiAxesLength { get; }
    private double Radius { get; }

    public Quaternion Rotation { get; set; } = Quaternion.NONE;

    public override Intersection GetIntersection(Line line, double minDist, double maxDist)
    {
        // Transform ray to ellipsoid's local coordinates
        var origin = line.X0 - Center;
        var direction = new Vector(line.Dx);

        // Scale the ray to transform the ellipsoid into a unit sphere
        var scaledOrigin = new Vector(origin.X / SemiAxesLength.X, origin.Y / SemiAxesLength.Y,
            origin.Z / SemiAxesLength.Z);
        var scaledDirection = new Vector(direction.X / SemiAxesLength.X, direction.Y / SemiAxesLength.Y,
            direction.Z / SemiAxesLength.Z);

        // Standard ray-sphere intersection (for a unit sphere at origin)
        var a = scaledDirection.Length2();
        var b = 2 * (scaledDirection * scaledOrigin);
        var c = scaledOrigin.Length2() - Radius * Radius;

        var delta = b * b - 4 * a * c;

        if (delta < 0) return Intersection.NONE;

        var sqrtDelta = Math.Sqrt(delta);
        var t1 = (-b - sqrtDelta) / (2 * a);
        var t2 = (-b + sqrtDelta) / (2 * a);

        double t = -1;

        if (t1 > minDist && t1 < maxDist)
            t = t1;
        else if (t2 > minDist && t2 < maxDist) t = t2;

        if (t < 0) return Intersection.NONE;

        var localPosition = scaledOrigin + scaledDirection * t;

        var normal = new Vector(
            2 * localPosition.X / (SemiAxesLength.X * SemiAxesLength.X),
            2 * localPosition.Y / (SemiAxesLength.Y * SemiAxesLength.Y),
            2 * localPosition.Z / (SemiAxesLength.Z * SemiAxesLength.Z)
        );
        normal.Normalize();

        var outwardNormal = normal;
        if (normal * line.Dx > 0) outwardNormal = normal * -1;

        return new Intersection(true, true, this, line, t, outwardNormal, Material, Color);
    }
}