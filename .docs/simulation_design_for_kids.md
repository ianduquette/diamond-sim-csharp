# How DiamondSim Thinks About Baseball (Simple Version)

Baseball is a game of tiny chances.
Each pitch can end up as a **strike**, a **ball**, or a **hit**.
DiamondSim pretends to play baseball by rolling digital dice for every pitch.

---

## How It Plays the Game
1. It starts with **zero balls and zero strikes**.
2. The pitcher throws.
   - If they have good **control**, it’s more likely to be a strike.
3. The batter decides whether to **swing**.
   - If they’re patient, they swing less often at bad pitches.
4. If they swing, another roll decides whether they **hit the ball**.
5. If they hit it, sometimes it’s **foul**, sometimes it’s **in play**.
   When it’s in play, the at-bat ends.

The count goes up:
- 3 balls → **walk**
- 2 strikes → **strikeout**

And that’s one at-bat.

---

## Why We Use a Seed (the Magic Dice Rule)
A seed is like using the same shuffle for a deck of cards every test.
It makes sure we can check if the math is right instead of guessing whether luck changed.

For fun or full games, we can turn the seed off—then it’s a new game every time.

---

## The Goal
| Outcome | What It Means | Target % |
|----------|---------------|----------|
| Strikeout | Batter misses or takes three strikes | 18–28% |
| Walk | Batter gets four balls | 7–12% |
| Ball in Play | Batter hits the ball somewhere | 55–70% |

We’re not copying exact stats; we’re matching the *feel* of the game.

---

## Why This Matters
We’re teaching the computer:
- How patience, power, and control change baseball.
- That every pitch has a story.
- And that “random” doesn’t mean “uncontrolled”—it means *possible.*
