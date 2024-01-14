using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SS14.Admin.Helpers;

/// <summary>
/// Helper class for displaying status messages on pages.
/// </summary>
/// <remarks>
/// <para>
/// Status messages are temporary popups that appear on a page after performing an action.
/// They are tracked with Temp Data in ASP.NET Core.
/// </para>
/// <para>
/// Pages display the message by using the <c>StatusMessageAlert</c> partial.
/// </para>
/// </remarks>
public static class StatusMessageHelper
{
    /// <summary>
    /// Key used to store the textual (user-displayed) message in the temporary data.
    /// </summary>
    public const string KeyMessage = "StatusMessage";

    /// <summary>
    /// Key used to store the type of status message (<see cref="Type"/>) in the temporary data.
    /// </summary>
    public const string KeyType = "StatusMessageType";

    /// <summary>
    /// Set the required keys in the temporary data dictionary to show a status message to a user.
    /// </summary>
    /// <param name="tempData">The temporary data dictionary to operate on.</param>
    /// <param name="message">The message displayed to the user.</param>
    /// <param name="type">The type of status message to display.</param>
    /// <seealso cref="SetStatusInformation"/>
    /// <seealso cref="SetStatusError"/>
    public static void SetStatusMessage(this ITempDataDictionary tempData, string message, Type type)
    {
        tempData[KeyMessage] = message;
        tempData[KeyType] = type;
    }

    /// <summary>
    /// Convenience function to set the status message to one of type <see cref="Type.Error"/>.
    /// </summary>
    /// <param name="tempData">The temporary data dictionary to operate on.</param>
    /// <param name="message">The message displayed to the user.</param>
    /// <seealso cref="SetStatusMessage"/>
    public static void SetStatusError(this ITempDataDictionary tempData, string message)
    {
        SetStatusMessage(tempData, message, Type.Error);
    }

    /// <summary>
    /// Convenience function to set the status message to one of type <see cref="Type.Information"/>.
    /// </summary>
    /// <param name="tempData">The temporary data dictionary to operate on.</param>
    /// <param name="message">The message displayed to the user.</param>
    /// <seealso cref="SetStatusMessage"/>
    public static void SetStatusInformation(this ITempDataDictionary tempData, string message)
    {
        SetStatusMessage(tempData, message, Type.Information);
    }

    /// <summary>
    /// Possible types of status messages displayed.
    /// </summary>
    /// <seealso cref="StatusMessageHelper.SetStatusMessage"/>
    public enum Type
    {
        /// <summary>
        /// Informational messages confirm successful actions to the user.
        /// </summary>
        Information,

        /// <summary>
        /// Error messages contain like... errors.
        /// </summary>
        /// <remarks>
        /// Like come on I wanna write doc comments, and at least <see cref="Information"/> demands
        /// *some* sort of description, but you all know what an error is.
        /// </remarks>
        Error
    }
}
