// ponytail: thin subclass kept through Stage 2 so the existing scene component (MatchThemAll.Scripts.Power_Ups.Vacuum)
// keeps resolving. All behavior + Animator/Started now live in the base Powerup. Deleted in Stage 3.
namespace MatchThemAll.Scripts.Power_Ups
{
    public class Vacuum : Powerup { }
}
