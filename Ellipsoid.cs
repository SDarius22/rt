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
        // Copy and normalize rotation
        var rot = new Quaternion(Rotation.W, Rotation.X, Rotation.Y, Rotation.Z).Normalize();
        var invRot = new Quaternion(rot.W, -rot.X, -rot.Y, -rot.Z); // conjugate (unit quaternion inverse)

        // Translate ray to ellipsoid center
        var localOrigin = new Vector(line.X0 - Center);
        var localDirection = new Vector(line.Dx);

        // Rotate ray into ellipsoid local frame
        localOrigin.Rotate(invRot);
        localDirection.Rotate(invRot);

        // Scale to unit sphere space
        var scaledOrigin = new Vector(
            localOrigin.X / SemiAxesLength.X,
            localOrigin.Y / SemiAxesLength.Y,
            localOrigin.Z / SemiAxesLength.Z
        );
        var scaledDirection = new Vector(
            localDirection.X / SemiAxesLength.X,
            localDirection.Y / SemiAxesLength.Y,
            localDirection.Z / SemiAxesLength.Z
        );

        // Ray-sphere intersection
        var a = scaledDirection.Length2();
        var b = 2.0 * (scaledDirection * scaledOrigin);
        var c = scaledOrigin.Length2() - Radius * Radius;

        var delta = b * b - 4.0 * a * c;
        if (delta < 0.0) return Intersection.NONE;

        var sqrtDelta = Math.Sqrt(delta);
        var t1 = (-b - sqrtDelta) / (2.0 * a);
        var t2 = (-b + sqrtDelta) / (2.0 * a);

        double t = -1.0;
        if (t1 > minDist && t1 < maxDist) t = t1;
        else if (t2 > minDist && t2 < maxDist) t = t2;

        if (t < 0.0) return Intersection.NONE;

        // Point in scaled (sphere) space
        var spherePoint = scaledOrigin + scaledDirection * t;

        // Normal in unrotated ellipsoid space (keep original formula)
        var normalLocal = new Vector(
            2.0 * spherePoint.X / (SemiAxesLength.X * SemiAxesLength.X),
            2.0 * spherePoint.Y / (SemiAxesLength.Y * SemiAxesLength.Y),
            2.0 * spherePoint.Z / (SemiAxesLength.Z * SemiAxesLength.Z)
        ).Normalize();

        // Flip if inside
        if (normalLocal * localDirection > 0.0)
            normalLocal = normalLocal * -1.0;

        // Rotate normal back to world space
        var worldNormal = new Vector(normalLocal);
        worldNormal.Rotate(rot);
        worldNormal.Normalize();

        return new Intersection(true, true, this, line, t, worldNormal, Material, Color);
    }
}