namespace Nop.Web.Models.Menus;

/// <summary>
/// Represents an event that occurs when a menu model is created
/// </summary>
public class MenuCreatedEvent
{
    #region Ctor

    public MenuCreatedEvent(MenuModel menu)
    {
        Menu = menu;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a menu model
    /// </summary>
    public MenuModel Menu { get; }

    #endregion
}
