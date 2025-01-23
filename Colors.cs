using Emgu.CV.Structure;

static class Colors
{
    public static readonly Tuple<MCvScalar, MCvScalar> redBounds =
        new Tuple<MCvScalar, MCvScalar>(new MCvScalar(0, 150, 150), new MCvScalar(10, 255, 255));
    public static readonly Tuple<MCvScalar, MCvScalar> purpleBounds =
        new Tuple<MCvScalar, MCvScalar>(new MCvScalar(140, 111, 160), new MCvScalar(148, 154, 194));
    public static readonly Tuple<MCvScalar, MCvScalar> yellowBounds =
        new Tuple<MCvScalar, MCvScalar>(new MCvScalar(30, 125, 150), new MCvScalar(30, 255, 255));

    public static Tuple<MCvScalar, MCvScalar> GetColorBounds(string name)
    {
        return name switch
        {
            "red" => redBounds,
            "purple" => purpleBounds,
            "yellow" => yellowBounds,
            _ => redBounds,
        };
    }

    public static Brush GetBrushFromColorName(string name)
    {
        // Obtém os limites da cor correspondente
        var bounds = GetColorBounds(name);

        // Converte o HSV médio para RGB
        var lower = bounds.Item1;
        var upper = bounds.Item2;

        // Calcula o HSV médio entre os limites
        double avgH = (lower.V0 + upper.V0) / 2;
        double avgS = (lower.V1 + upper.V1) / 2;
        double avgV = (lower.V2 + upper.V2) / 2;

        // Converte HSV para RGB
        Color rgbColor = HsvToRgb(avgH, avgS, avgV);

        // Retorna um SolidBrush com a cor correspondente
        return new SolidBrush(rgbColor);
    }

    private static Color HsvToRgb(double h, double s, double v)
    {
        // Normaliza HSV
        h = h / 360.0; // Hue varia de 0 a 360
        s = s / 255.0; // Saturação varia de 0 a 255
        v = v / 255.0; // Valor varia de 0 a 255

        double r = 0, g = 0, b = 0;

        int i = (int)Math.Floor(h * 6);
        double f = h * 6 - i;
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);

        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        // Converte para 0-255
        return Color.FromArgb(
            (int)Math.Round(r * 255),
            (int)Math.Round(g * 255),
            (int)Math.Round(b * 255)
        );
    }
}