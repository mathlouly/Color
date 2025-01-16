using Emgu.CV.Structure;

static class Colors
{
    public static readonly Color redColor = Color.FromArgb(255, 239, 39, 41);
    public static readonly Color purpleColor = Color.FromArgb(255, 150, 70, 254);
    public static readonly Color yellowColor = Color.FromArgb(255, 254, 254, 35);
    public static readonly Color yellowColor2 = Color.FromArgb(255, 254, 254, 30);

    public static Color GetColorByName(string name)
    {
        return name switch
        {
            "red" => redColor,
            "purple" => purpleColor,
            "yellow" => yellowColor,
            "yellow2" => yellowColor2,
            _ => purpleColor,
        };
    }
}

static class Colors2
{
    public static readonly Tuple<MCvScalar, MCvScalar> redBounds =
        new Tuple<MCvScalar, MCvScalar>(new MCvScalar(0, 150, 150), new MCvScalar(10, 255, 255));
    public static readonly Tuple<MCvScalar, MCvScalar> purpleBounds =
        new Tuple<MCvScalar, MCvScalar>(new MCvScalar(130, 50, 100), new MCvScalar(130, 255, 255));
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
}