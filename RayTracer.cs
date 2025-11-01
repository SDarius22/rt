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
        var incidentLine = new Line(point, light.Position);

        // Define a small value for numerical stability
        const double epsilon = 1e-10;

        // Calculate the length of the line segment between the point and the light source
        var segmentLength = (point - light.Position).Length();

        // Check for intersections with scene geometries
        foreach (var geometry in geometries)
        {
            // Skip RawCtMask geometry
            if (geometry is CtScan) continue;

            // Check for intersection along the incident line within a limited segment
            var intersection = geometry.GetIntersection(incidentLine, epsilon, segmentLength);

            // If an intersection is visible, the point is not lit
            if (intersection.Visible) return false;
        }

        return true;
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
                var pointOnViewPlane = camera.Position + camera.Direction * camera.ViewPlaneDistance + 
                                       (camera.Up ^ camera.Direction) * ImageToViewPlane(i, width, camera.ViewPlaneWidth) + 
                                       camera.Up * ImageToViewPlane(j, height, camera.ViewPlaneHeight);
                var ray = new Line(camera.Position, pointOnViewPlane);

                // Find the first intersection of the ray with scene geometries
                var intersection = FindFirstIntersection(ray, camera.FrontPlaneDistance, camera.BackPlaneDistance);

                // If a visible intersection is found, calculate set the pixel color in the rendered image
                if (intersection.Valid && intersection.Visible)
                {
                    // Extract material and surface properties from the intersection
                    // These values are used to calculate the pixel color
                    var material = intersection.Material;
                    var pixelColor = new Color();
                    var pointOnSurface = intersection.Position;
                    var eyeVector = (camera.Position - pointOnSurface).Normalize();
                    var surfaceNormal = intersection.Normal;

                    // Iterate over each light source to calculate lighting contributions
                    foreach (var light in lights)
                    {
                        // Calculate ambient component
                        var ambientComponent = material.Ambient * light.Ambient;

                        // Check if the point on the surface is lit by the current light source
                        if (IsLit(pointOnSurface, light))
                        {
                            var lightDirection = (light.Position - pointOnSurface).Normalize();
                            var reflectionDirection =
                                (surfaceNormal * (surfaceNormal * lightDirection) * 2 - lightDirection).Normalize();

                            var diffuseFactor = surfaceNormal * lightDirection;
                            var specularFactor = eyeVector * reflectionDirection;

                            if (diffuseFactor > 0)
                                pixelColor += material.Diffuse * light.Diffuse * diffuseFactor;

                            if (specularFactor > 0)
                                pixelColor += material.Specular * light.Specular *
                                              Math.Pow(specularFactor, material.Shininess);
                        }

                        // Add ambient component to the pixel color
                        pixelColor += ambientComponent;
                    }

                    
                    image.SetPixel(i, j, pixelColor);
                    continue;
                }
                
                // If no visible intersection is found, set the normal background color
                image.SetPixel(i, j, background);
            }
        }


        image.Store(filename);
    }
}