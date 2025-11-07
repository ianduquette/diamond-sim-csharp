namespace DiamondSim.Tests.TestHelpers;

/// <summary>
/// Test helper for measuring contact rates using a ContactResolver.
/// </summary>
public static class ContactResolverTestHelper {
    /// <summary>
    /// Counts the number of contacts made over multiple trials at a specific count.
    /// </summary>
    /// <param name="resolver">The contact resolver to test.</param>
    /// <param name="batter">The batter in the matchup.</param>
    /// <param name="pitcher">The pitcher in the matchup.</param>
    /// <param name="trials">The number of trials to run.</param>
    /// <param name="balls">The number of balls in the count (0-3). Defaults to 0.</param>
    /// <param name="strikes">The number of strikes in the count (0-2). Defaults to 0.</param>
    /// <returns>The number of contacts made out of the total trials.</returns>
    public static int CountContacts(
        ContactResolver resolver,
        Batter batter,
        Pitcher pitcher,
        int trials,
        int balls = 0,
        int strikes = 0) {

        var contacts = 0;
        for (var i = 0; i < trials; i++) {
            if (resolver.MakesContact(batter.Ratings, pitcher.Ratings, balls, strikes)) {
                contacts++;
            }
        }
        return contacts;
    }

    /// <summary>
    /// Measures the contact rate (as a decimal between 0 and 1) over multiple trials at a specific count.
    /// </summary>
    /// <param name="resolver">The contact resolver to test.</param>
    /// <param name="batter">The batter in the matchup.</param>
    /// <param name="pitcher">The pitcher in the matchup.</param>
    /// <param name="trials">The number of trials to run.</param>
    /// <param name="balls">The number of balls in the count (0-3). Defaults to 0.</param>
    /// <param name="strikes">The number of strikes in the count (0-2). Defaults to 0.</param>
    /// <returns>The contact rate as a decimal (contacts / trials).</returns>
    public static double MeasureContactRate(
        ContactResolver resolver,
        Batter batter,
        Pitcher pitcher,
        int trials,
        int balls = 0,
        int strikes = 0) {

        var contacts = CountContacts(resolver, batter, pitcher, trials, balls, strikes);
        return (double)contacts / trials;
    }

    /// <summary>
    /// Measures the contact rate for average batter vs average pitcher at a specific count.
    /// Convenience overload that creates average players internally.
    /// </summary>
    /// <param name="resolver">The contact resolver to test.</param>
    /// <param name="trials">The number of trials to run.</param>
    /// <param name="balls">The number of balls in the count (0-3). Defaults to 0.</param>
    /// <param name="strikes">The number of strikes in the count (0-2). Defaults to 0.</param>
    /// <returns>The contact rate as a decimal (contacts / trials).</returns>
    public static double MeasureContactRate(
        ContactResolver resolver,
        int trials,
        int balls = 0,
        int strikes = 0) {

        var batter = TestFactory.CreateAverageBatter();
        var pitcher = TestFactory.CreateAveragePitcher();
        return MeasureContactRate(resolver, batter, pitcher, trials, balls, strikes);
    }
}
