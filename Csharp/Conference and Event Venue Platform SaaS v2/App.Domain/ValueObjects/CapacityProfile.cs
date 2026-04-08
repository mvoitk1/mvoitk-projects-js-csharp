namespace App.Domain.ValueObjects;

public class CapacityProfile
{
    public int Minimum { get; private set; }
    public int Recommended { get; private set; }
    public int Maximum { get; private set; }

    private CapacityProfile()
    {
    }

    public CapacityProfile(int minimum, int recommended, int maximum)
    {
        if (minimum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimum));
        }

        if (recommended < minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(recommended));
        }

        if (maximum < recommended)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }

        Minimum = minimum;
        Recommended = recommended;
        Maximum = maximum;
    }

    public static CapacityProfile Empty() => new(0, 0, 0);
}
