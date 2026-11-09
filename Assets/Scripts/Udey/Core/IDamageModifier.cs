namespace Udey.Core
{
    /// Put one of these on the same GameObject as a Health, and it gets a chance to change
    /// incoming damage before it lands. The Tank's FrontalShield is the main example.
    /// Health applies every modifier it finds, in component order.
    public interface IDamageModifier
    {
        /// <param name="incoming">Damage so far, after any earlier modifiers.</param>
        /// <param name="info">The original hit, for direction / type checks.</param>
        /// <returns>The damage that should actually be dealt.</returns>
        float ModifyDamage(float incoming, DamageInfo info);
    }
}
