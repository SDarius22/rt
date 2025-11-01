using System;
using System.IO;
using System.Text.RegularExpressions;

namespace rt;

public class CtScan : Geometry
{
    private readonly ColorMap _colorMap;
    private readonly byte[] _data;
    private readonly Vector _position;

    private readonly int[] _resolution = new int[3];
    private readonly double _scale;
    private readonly double[] _thickness = new double[3];
    private readonly Vector _v0;
    private readonly Vector _v1;

    public CtScan(string datFile, string rawFile, Vector position, double scale, ColorMap colorMap) : base(Color.NONE)
    {
        _position = position;
        _scale = scale;
        _colorMap = colorMap;

        var lines = File.ReadLines(datFile);
        foreach (var line in lines)
        {
            var kv = Regex.Replace(line, "[:\\t ]+", ":").Split(":");
            if (kv[0] == "Resolution")
            {
                _resolution[0] = Convert.ToInt32(kv[1]);
                _resolution[1] = Convert.ToInt32(kv[2]);
                _resolution[2] = Convert.ToInt32(kv[3]);
            }
            else if (kv[0] == "SliceThickness")
            {
                _thickness[0] = Convert.ToDouble(kv[1]);
                _thickness[1] = Convert.ToDouble(kv[2]);
                _thickness[2] = Convert.ToDouble(kv[3]);
            }
        }

        _v0 = position;
        _v1 = position + new Vector(_resolution[0] * _thickness[0] * scale, _resolution[1] * _thickness[1] * scale,
            _resolution[2] * _thickness[2] * scale);

        var len = _resolution[0] * _resolution[1] * _resolution[2];
        _data = new byte[len];
        using var f = new FileStream(rawFile, FileMode.Open, FileAccess.Read);
        if (f.Read(_data, 0, len) != len) throw new InvalidDataException($"Failed to read the {len}-byte raw data");
    }

    private ushort Value(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= _resolution[0] || y >= _resolution[1] || z >= _resolution[2]) return 0;

        return _data[z * _resolution[1] * _resolution[0] + y * _resolution[0] + x];
    }

    public override Intersection GetIntersection(Line line, double minDist, double maxDist)
    {
        var tMin = double.NegativeInfinity;
        var tMax = double.PositiveInfinity;

        // X slab
        if (Math.Abs(line.Dx.X) > 1e-6)
        {
            var t1 = (_v0.X - line.X0.X) / line.Dx.X;
            var t2 = (_v1.X - line.X0.X) / line.Dx.X;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = Math.Max(tMin, t1);
            tMax = Math.Min(tMax, t2);
        }
        else if (line.X0.X < _v0.X || line.X0.X > _v1.X)
        {
            return Intersection.NONE;
        }

        // Y slab
        if (Math.Abs(line.Dx.Y) > 1e-6)
        {
            var t1 = (_v0.Y - line.X0.Y) / line.Dx.Y;
            var t2 = (_v1.Y - line.X0.Y) / line.Dx.Y;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = Math.Max(tMin, t1);
            tMax = Math.Min(tMax, t2);
        }
        else if (line.X0.Y < _v0.Y || line.X0.Y > _v1.Y)
        {
            return Intersection.NONE;
        }

        // Z slab
        if (Math.Abs(line.Dx.Z) > 1e-6)
        {
            var t1 = (_v0.Z - line.X0.Z) / line.Dx.Z;
            var t2 = (_v1.Z - line.X0.Z) / line.Dx.Z;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = Math.Max(tMin, t1);
            tMax = Math.Min(tMax, t2);
        }
        else if (line.X0.Z < _v0.Z || line.X0.Z > _v1.Z)
        {
            return Intersection.NONE;
        }

        if (tMin > tMax) return Intersection.NONE;

        // The ray is inside the box between tMin and tMax
        // We must also respect the minDist and maxDist parameters
        var tStart = Math.Max(tMin, minDist);
        var tEnd = Math.Min(tMax, maxDist);

        if (tStart >= tEnd) return Intersection.NONE;

        // Ray marching
        var step = Math.Max(0.001, Math.Min(_thickness[0], Math.Min(_thickness[1], _thickness[2])) * _scale);
        for (var t = tStart; t < tEnd; t += step)
        {
            var currentPos = line.CoordinateToPosition(t);
            var color = GetColor(currentPos);

            // If we hit something that is not fully transparent
            if (color.Alpha > 1e-5)
            {
                var normal = GetNormal(currentPos);
                return new Intersection(true, true, this, line, t, normal, Material.FromColor(color), color);
            }
        }

        return Intersection.NONE;
    }

    private int[] GetIndexes(Vector v)
    {
        return new[]
        {
            (int)Math.Floor((v.X - _position.X) / _thickness[0] / _scale),
            (int)Math.Floor((v.Y - _position.Y) / _thickness[1] / _scale),
            (int)Math.Floor((v.Z - _position.Z) / _thickness[2] / _scale)
        };
    }

    private Color GetColor(Vector v)
    {
        var idx = GetIndexes(v);

        var value = Value(idx[0], idx[1], idx[2]);
        return _colorMap.GetColor(value);
    }

    private Vector GetNormal(Vector v)
    {
        var idx = GetIndexes(v);
        double x0 = Value(idx[0] - 1, idx[1], idx[2]);
        double x1 = Value(idx[0] + 1, idx[1], idx[2]);
        double y0 = Value(idx[0], idx[1] - 1, idx[2]);
        double y1 = Value(idx[0], idx[1] + 1, idx[2]);
        double z0 = Value(idx[0], idx[1], idx[2] - 1);
        double z1 = Value(idx[0], idx[1], idx[2] + 1);

        return new Vector(x1 - x0, y1 - y0, z1 - z0).Normalize();
    }
}