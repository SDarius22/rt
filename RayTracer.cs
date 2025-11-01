using System;

namespace rt;

internal class RayTracer(Geometry[] geometries, Light[] lights)
{
    private double ImageToViewPlane(int n, int imgSize, double viewPlaneSize)
    {
        return -n * viewPlaneSize / imgSize + viewPlaneSize / 2;
    }

    private Intersection FindFirstIntersection(Line ray, double minDist, double maxDist)
    {
        var intersection = Intersection.NONE;

        foreach (var geometry in geometries)
        {
            var intr = geometry.GetIntersection(ray, minDist, maxDist);

            if (!intr.Valid || !intr.Visible) continue;

            if (!intersection.Valid || !intersection.Visible)
                intersection = intr;
            else if (intr.T < intersection.T) intersection = intr;
        }

        return intersection;
    }

    private bool IsLit(Vector point, Light light)
    {
        var direction = (light.Position - point).Normalize();
        var shadowRay = new Line(point, direction);
        var distanceToLight = (light.Position - point).Length();
        var intersection = FindFirstIntersection(shadowRay, 0.001, distanceToLight - 0.001);
        return !intersection.Valid;
    }

    public void Render(Camera camera, int width, int height, string filename)
    {
        var background = new Color(0.2, 0.2, 0.2, 1.0);

        var image = new Image(width, height);

        for (var i = 0; i < width; i++)
        {
            if (i % 100 == 0) Console.WriteLine($"Rendering column {i}/{width}");

            for (var j = 0; j < height; j++)
            {
                var f = camera.Direction.Normalize();
                var r = (f ^ camera.Up).Normalize();
                var u = (r ^ f).Normalize();

                var aspect = (double)width / height;
                var vpH = camera.ViewPlaneHeight;
                var vpW = camera.ViewPlaneWidth > 0 ? camera.ViewPlaneWidth : vpH * aspect;

                var origin = camera.Position;
                var center = origin + f * camera.ViewPlaneDistance;
                var topLeft = center + u * (vpH * 0.5) - r * (vpW * 0.5);
                var stepX = r * (vpW / width);
                var stepY = u * (vpH / height) * -1;

                var p = topLeft + stepX * (i + 0.5) + stepY * (j + 0.5);
                var dir = (p - origin).Normalize();

                var ray = new Line(origin, origin + dir);

                var hit = FindFirstIntersection(ray, camera.FrontPlaneDistance, camera.BackPlaneDistance);

                if (hit.Valid && hit.Visible)
                {
                    var color = new Color();
                    var material = hit.Material;

                    foreach (var light in lights)
                    {
                        color += hit.Color * material.Ambient * light.Ambient;

                        if (IsLit(hit.Position, light))
                        {
                            var L = (light.Position - hit.Position).Normalize();
                            var N = hit.Normal.Normalize();

                            var ndotl = N * L;
                            if (ndotl > 0)
                                color += hit.Color * material.Diffuse * light.Diffuse * ndotl;

                            var R = (N * (2 * ndotl) - L).Normalize();
                            var Vdot = R * (dir * -1);
                            if (Vdot > 0)
                                color += material.Specular * light.Specular * Math.Pow(Vdot, material.Shininess);
                        }
                    }

                    image.SetPixel(i, j, color);
                    continue;
                }

                image.SetPixel(i, j, background);
            }
        }


        image.Store(filename);
    }
}