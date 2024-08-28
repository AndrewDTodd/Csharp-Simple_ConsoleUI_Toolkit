/*
ConsolePage
<summary>
This class defines the page structure used by the "ConsoleUIManager" instance
Represents a meaningfull collection of prompts to allow the user to navigate and direct the applications state/setting
</summary>

NavigationInteraction
<summary>
Used by the "ConsolePage" to define the navagation items of the page
</summary>

InputInteraction
<summary>
Used by the <seealso "ConsolePage" to handle input
</summary>

Copyright 2023 Andrew Todd

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE
AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI_Toolkit
{
    /// <summary>
    /// <para>This class defines the page structure used by the <seealso cref="ConsoleUIManager"/> instance</para>
    /// <para>Represents a meaningfull collection of prompts to allow the user to navigate and direct the applications state/setting</para>
    /// <para>Allows for the handling of user-input requests by the program logic</para>
    /// </summary>
    public class ConsolePage
    {
        #region PUBLIC_STATIC_METHODS
        public static bool IsVisible()
        {
            return true;
        }
        #endregion

        #region PROPERTIES
        public string PageName { get; init; }
        public string? PagePrompts { get; init; }
        public NavigationInteraction[]? NavigationInteractions {get; init;}
        public NavigationInteraction GoBackInteraction { get; init; }
        public DisplayField[]? ObjectsToDisplay { get; init; }
        public InputInteraction? InputInteraction { get; init; }
        #endregion

        #region CONSTRUCTORS
        public ConsolePage(NavigationInteraction goBack, string pageName, string? pagePrompts = null,
            NavigationInteraction[]? navigationInteractions = null, DisplayField[]? objectsToDisplay = null, InputInteraction? inputInteraction = null) =>
            (GoBackInteraction, PageName, PagePrompts, NavigationInteractions, ObjectsToDisplay, InputInteraction) = (goBack, pageName, pagePrompts, navigationInteractions, objectsToDisplay, inputInteraction);
        #endregion
    }

    /// <summary>
    /// Used by the <seealso cref="ConsolePage"/> to define the navagation items of the page
    /// </summary>
    public class NavigationInteraction
    {
        #region PROPERTIES
        public string? InteractionPrompt { get; init; }
        public string InteractionText { get; init; }
        public ConsoleKey[] InteractionKey { get; init; }
        public Func<bool> InteractionAction { get; init; }
        public Func<bool> InteractionVisible { get; init; }
        #endregion

        #region CONSTRUCTORS
        public NavigationInteraction(string interactionText, ConsoleKey[] interactionKey, Func<bool> interactionAction, Func<bool> isVisible, string? interactionPrompt = null) =>
            (InteractionPrompt, InteractionText, InteractionAction, InteractionVisible, InteractionKey) = (interactionPrompt, interactionText, interactionAction, isVisible, interactionKey);
        #endregion
    }

    /// <summary>
    /// Used by the <seealso cref="ConsolePage"/> to handle input
    /// </summary>
    public class InputInteraction
    {
        #region PROPERTIES
        public string InputPrompt { get; init; }
        public Func<string, bool> InputAction { get; init; }
        public Func<bool> InteractionVisible { get; init; }
        #endregion

        #region CONSTRUCTORS
        public InputInteraction(string inputPrompt, Func<string, bool> inputAction, Func<bool> interactionVisible) =>
            (InputPrompt, InputAction, InteractionVisible) = (inputPrompt, inputAction, interactionVisible);
        #endregion
    }

    /// <summary>
    /// Used by the <seealso cref="ConsolePage"/> to link information from the logic to the UI
    /// </summary>
    public class DisplayField
    {
        #region PROPERTIES
        public string FieldMessage { get; init; }
        public object ObjectToDisplay { get; init; }
        public Func<string> ObjectDisplayFunction { get; init; }
        public Func<bool> InteractionVisible { get; init; }
        #endregion

        #region CONSTRUCTORS
        public DisplayField(string message, object toDisplay, Func<string> displayFunc, Func<bool> interactionVisible) =>
            (FieldMessage, ObjectToDisplay, ObjectDisplayFunction, InteractionVisible) = (message, toDisplay, displayFunc, interactionVisible);
        #endregion
    }
}
