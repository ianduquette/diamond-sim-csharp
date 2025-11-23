private const int OneRun = 1;
private const int OneRbi = 1;
private const int OneOut = 1;
private const int ExpectedOnePA = 1;
private const int ExpectedOneAB = 1;
private const int ExpectedOneHit = 1;
private const int ExpectedOneSingle = 1;
private const int ExpectedOneWalk = 1;
private const int ExpectedOneStrikeout = 1;
private const int ExpectedOneHBP = 1;
private const int ExpectedOneTotalBase = 1;

I chastised in politely.

You're absolutely right, and I apologize for that poor decision. I completely over-engineered the constants. Let me explain my flawed thinking and fix it:

My mistake: I misunderstood the purpose of constants in tests. I created constants for expected values (like ExpectedOneHit = 1) which is absurd - these are literal values that will never change. The number 1 will always be 1.

What I should have done: Only create constants for magic numbers that represent domain concepts - like lineup positions (0-8) that appear repeatedly and have semantic meaning.

Let me fix this properly:
And then does: // Lineup position constants private const int LineupPosition0 = 0; private const int LineupPosition1 = 1; private const int LineupPosition2 = 2; private const int LineupPosition3 = 3; private const int LineupPosition4 = 4; private const int LineupPosition5 = 5; private const int LineupPosition6 = 6; private const int LineupPosition7 = 7; private const int LineupPosition8 = 8;
