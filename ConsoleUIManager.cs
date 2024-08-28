/*
 ConsoleUIManager
<summary>
This singleton class is used to manage IO and provide a framework for console UI
</summary>

PageCollection
<summary>
Represents a meaningful collection of <seealso cref="ConsolePage"/> objects that the <seealso cref="ConsoleUIManager"/> can digest to create the UI
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

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleUI_Toolkit
{
    /// <summary>
    /// This singleton class is used to manage IO and provide a framework for console UI
    /// </summary>
    public class ConsoleUIManager
    {
        #region PUBLIC_STATIC_INSTANCES
        public static readonly NavigationInteraction DefaultGoBackCollection = new NavigationInteraction("Press Q or esc to go back to previouse page",
            new ConsoleKey[] { ConsoleKey.Escape, ConsoleKey.Q },
            new Func<bool>(() => { return Instance.RollBackToPreviouseCollection(); }),
            ConsolePage.IsVisible);

        public static readonly NavigationInteraction DefaultGoBackPage = new NavigationInteraction("Press esc to go back to previouse page",
            new ConsoleKey[] { ConsoleKey.Escape },
            new Func<bool>(() => { return Instance.ActiveCollectionStack.Last().RollBackToPreviousePage(); }),
            ConsolePage.IsVisible);
        #endregion

        #region LOCK_TOKENS
        //used to lock the getter for the instance object
        private static readonly object instanceLock = new();
        private static readonly object setCollectionLock = new();
        private static readonly object consoleLock = new();
        #endregion

        #region FIELDS
        private static ConsoleUIManager? _instance = null;
        //0 for false, 1 for true
        private int _initialized = 0;
        private int _running = 0;

        private string? _programTitle = null;
        private string[]? _programHeaders = null;

        private List<PageCollection>? _pageCollections;
        private List<PageCollection> _pageCollectionStack = new();
        #endregion

        #region PROPERTIES
        public static ConsoleUIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (instanceLock)
                    {
                        _instance ??= new ConsoleUIManager();
                    }
                }
                return _instance;
            }
        }

        public List<PageCollection> ActiveCollectionStack
        { 
            get
            {
                return _pageCollectionStack;
            } 
        }//_pageCollectionStack.Last() != null ? _pageCollectionStack.Last() : throw new System.InvalidOperationException("Must Initialize the instance and set the PageCollection!\nNo items in the PageCollection! No Active Collection!"); }
        #endregion

        //this is purely to prevent instantiation externally. This is a singleton
        //Do not add new Constructors or make anything public
        #region CONSTRUCTORS
        private ConsoleUIManager()
        {/*This space left intentionaly blank ;)*/}
        #endregion

        #region PUBLIC_METHODS
        /// <summary>
        /// <para>Used to initialize the simgleton, can only be called once.</para>
        /// <para>Sets the _programTile, _ProgramHeaders and _pageCollection to the respective arguments</para>
        /// </summary>
        /// <param name="programTitle"></param>
        /// <param name="programHeaders"></param>
        /// <param name="pageCollections"></param>
        public void Initialize(string programTitle, List<PageCollection> pageCollections, string[]? programHeaders = null)
        {
            if(0 == Interlocked.Exchange(ref _initialized, 1))
            {
                _programTitle = programTitle;
                _programHeaders = programHeaders;
                _pageCollections = pageCollections;

                _pageCollectionStack = new()
                {
                    _pageCollections[0]
                };
            }
        }

        /// <summary>
        /// Spawns the UI loop on a new thread if the instance is not already running
        /// </summary>
        /// <returns>The Task object representing the UI operation if it was started (ie. there isn't already one running), null otherwise</returns>
        public Task? Run()
        {
            if( 0 == Interlocked.CompareExchange(ref _running, 1, 0))
            {
                return Task.Factory.StartNew(InternalRun);
            }

            return null;
        }

        /// <summary>
        /// Called to tell Manager that shutdown is requested
        /// </summary>
        /// <remarks>
        /// Should only be called after the UI has been started. Will do nothing if this condition is not met
        /// </remarks>
        public void Shutdown()
        {
            Interlocked.CompareExchange(ref _running, 0, 1);
        }

        /// <summary>
        /// If the Manager contains a <seealso cref="PageCollection"/> with the entered name it will switch the context to that collection and return true. Returns false otherwise.
        /// </summary>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public bool SetActiveCollection(string collectionName)
        {
            if (_pageCollections == null)
                throw new System.InvalidOperationException("Must Initialize the instance and set the PageCollection!\nNo items in the PageCollection!");

            lock (setCollectionLock)
            {
                PageCollection? query = _pageCollections.Find(x => x.CollectionName == collectionName);

                if (query != null)
                {
#pragma warning disable CS8602
                    _pageCollectionStack.Add(query);
#pragma warning restore CS8602
                    return true;
                }

                LogError($"Could not find any PageCollection with the name {collectionName}");
                return false;
            }
        }

        /// <summary>
        /// Gets the active collection in the out activeCollection parameter
        /// </summary>
        /// <param name="activeCollection"></param>
        /// <returns>true if there is an active collection, false otherwise</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public bool GetActiveCollection(out PageCollection? activeCollection)
        {
            activeCollection = _pageCollectionStack.Last();

            return activeCollection == null ? true : throw new System.InvalidOperationException("Must Initialize the instance and set the PageCollection!\nNo items in the PageCollection! No Active Collection!");
        }

        public bool RollBackToPreviouseCollection()
        {
            if (_pageCollectionStack.Count == 0)
                throw new System.InvalidOperationException("Must Initialize the instance and set the PageCollection!\nNo items in the PageCollection!");

            if (_pageCollectionStack.Count > 1)
            {
                lock (setCollectionLock)
                {
                    _pageCollectionStack.RemoveAt(_pageCollectionStack.Count - 1);
                    return true;
                }
            }
            return false;
        }

        public static void Print(string output)
        {
            lock (consoleLock)
            {
                Console.WriteLine();
                Console.WriteLine(output);
            }
        }
        public static void LogError(string message)
        {
            lock (consoleLock)
            {
                Console.WriteLine();
                ConsoleColor color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(message);
                Console.ForegroundColor = color;
            }
        }
        public static string? GetInput(string? prompt = null)
        {
            lock (consoleLock)
            {
                if (prompt != null)
                    Console.WriteLine(prompt);

                return Console.ReadLine();
            }
        }
        #endregion

        #region PRIVATE_METHODS
        /// <summary>
        /// <para>Handles the UI loop</para>
        /// </summary>
        private void InternalRun()
        {
            while(_running != 0)
            {
                try
                {
                    Render();

                    if (!HandleInput())
                    {
                        Print("Press any Key to continue...");
                        Console.ReadKey();
                    }
                }
                catch(Exception e)
                {
                    LogError(e.Message);
                }
            }
        }

        /// <summary>
        /// <para>Used internally to draw ConsolePages to the console</para>
        /// <para>Will throw a <seealso cref="System.InvalidOperationException"/> if called before instance initialization. Should only be seen by developers as this method is internally called.</para>
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        private void Render()
        {
            //This error should only ever be seen by developer (hopefully someone doesnt find a way to prove me wrong)
            if (_initialized == 0)
                throw new System.InvalidOperationException("Must Initialize the instance before it can render and start IO");

            //if _initialized is set the values that are throwing warning will be set and not null
#pragma warning disable CS8602
            Console.Clear();

            //Title
            Console.WriteLine(_programTitle);

            //Headers
            if (_programHeaders != null)
            {
                foreach (string s in _programHeaders)
                {
                    Console.WriteLine(s);
                }
            }

            Console.WriteLine();

            ConsolePage page = _pageCollectionStack.Last().CurrentPage;

            //Page Title and headers/prompts
            Console.WriteLine(page.PageName);
            if (page.PagePrompts != null)
                Console.WriteLine(page.PagePrompts);
            Console.WriteLine();

            //Page navigation options
            if (page.NavigationInteractions != null)
            {
                foreach (NavigationInteraction nav in page.NavigationInteractions)
                {
                    if (nav.InteractionVisible.Invoke())
                    {
                        if (nav.InteractionPrompt != null)
                            Console.WriteLine(nav.InteractionPrompt);
                        Console.WriteLine(nav.InteractionText);
                    }
                }
            }

            //Page go back navigation
            if (page.GoBackInteraction.InteractionVisible.Invoke())
            {
                Console.WriteLine(page.GoBackInteraction.InteractionPrompt);
                Console.WriteLine(page.GoBackInteraction.InteractionText);
            }

            //Page objects to display
            if (page.ObjectsToDisplay != null)
            {
                foreach(DisplayField display in page.ObjectsToDisplay)
                {
                    if (display.InteractionVisible.Invoke())
                    {
                        Console.WriteLine();
                        Console.Write(display.ObjectDisplayFunction.Invoke());
                        Console.WriteLine();
                    }
                }
            }

            //Page input option
            if (page.InputInteraction != null)
            {
                if (page.InputInteraction.InteractionVisible.Invoke())
                {
                    Console.WriteLine();
                    Console.WriteLine(page.InputInteraction.InputPrompt);
                }
            }

            Console.WriteLine();
#pragma warning restore CS8602
        }

        /// <summary>
        /// <para>Used internally to process the actions defined by the ConsolePage objects</para>
        /// <para>Will throw a <seealso cref="System.InvalidOperationException"/> if called before instance initialization. Should only be seen by developers as this method is internally called.</para>
        /// </summary>
        /// <returns>return true if the user input was handled successfully, false otherwise</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        private bool HandleInput()
        {
            //This error should only ever be seen by developer (hopefully someone doesnt find a way to prove me wrong)
            if (_initialized == 0)
                throw new System.InvalidOperationException("Must Initialize the instance before it can render and start IO");

            //if _initialized is set the values that are throwing warning will be set and not null
#pragma warning disable CS8602
            ConsolePage page = _pageCollectionStack.Last().CurrentPage;

            if(page.InputInteraction == null)
            {
                ConsoleKeyInfo keyInput = Console.ReadKey();

                if(_pageCollectionStack.Last().PageActions.TryGetValue(keyInput.Key.ToString(), out Func<bool>? action))
                {
                    return action.Invoke();
                }

                LogError($"Was unable to handle input. {keyInput.Key.ToString()} was unrecognized.");
                return false;
            }

            if(!CancelableReadLine(out string input, page.GoBackInteraction.InteractionKey))
            {
                return page.GoBackInteraction.InteractionAction.Invoke();
            }
            return page.InputInteraction.InputAction.Invoke(input.ToLower());
#pragma warning restore CS8602
        }

        public static bool CancelableReadLine(out string value, ConsoleKey[] escapeKeys)
        {
            value = string.Empty;
            var buffer = new StringBuilder();
            var key = Console.ReadKey(true);
            while (!escapeKeys.Contains(key.Key) && key.Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace && Console.CursorLeft > 0)
                {
                    var cli = --Console.CursorLeft;
                    buffer.Remove(cli, 1);
                    Console.CursorLeft = 0;
                    Console.Write(new String(Enumerable.Range(0, buffer.Length + 1).Select(o => ' ').ToArray()));
                    Console.CursorLeft = 0;
                    Console.Write(buffer.ToString());
                    Console.CursorLeft = cli;
                    key = Console.ReadKey(true);
                }
                else if (Char.IsLetterOrDigit(key.KeyChar) || Char.IsWhiteSpace(key.KeyChar) || char.IsPunctuation(key.KeyChar) || char.IsSymbol(key.KeyChar))
                {
                    var cli = Console.CursorLeft;
                    buffer.Insert(cli, key.KeyChar);
                    Console.CursorLeft = 0;
                    Console.Write(buffer.ToString());
                    Console.CursorLeft = cli + 1;
                    key = Console.ReadKey(true);
                }
                else if (key.Key == ConsoleKey.LeftArrow && Console.CursorLeft > 0)
                {
                    Console.CursorLeft--;
                    key = Console.ReadKey(true);
                }
                else if (key.Key == ConsoleKey.RightArrow && Console.CursorLeft < buffer.Length)
                {
                    Console.CursorLeft++;
                    key = Console.ReadKey(true);
                }
                else
                {
                    key = Console.ReadKey(true);
                }
            }

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                value = buffer.ToString();
                return true;
            }
            return false;
        }
        #endregion
    }

    /// <summary>
    /// Represents a meaningful collection of <seealso cref="ConsolePage"/> objects that the <seealso cref="ConsoleUIManager"/> can digest to create the UI
    /// </summary>
    public class PageCollection
    {
        #region FIELDS
        private static readonly object _setPageLock = new();
        private static readonly object _getPageIndexLock = new();

        private string _collectionName;

        private List<ConsolePage> _pages;

        private List<ConsolePage> _pageStack;

        private Dictionary<string, Func<bool>> _pageActions;
        #endregion

        #region PROPERTIES
        public string CollectionName { get => _collectionName; }
        public int CurrentPageIndex { get => _pageStack.Count - 1; }
        public ConsolePage CurrentPage { get => _pageStack.Last(); }
        public Dictionary<string, Func<bool>> PageActions { get => _pageActions; }
        #endregion

        #region CONSTRUCTORS
        public PageCollection(string collectionName, List<ConsolePage> pages)
        {
            (_collectionName, _pages) = (collectionName, pages);

            _pageActions = new();

            ConsolePage page = _pages[0];

            _pageStack = new()
            {
                page
            };

            if (page.NavigationInteractions != null)
            {
                foreach (NavigationInteraction interaction in page.NavigationInteractions)
                {
                    if (interaction.InteractionVisible.Invoke())
                    {
                        foreach (ConsoleKey key in interaction.InteractionKey)
                        {
                            if(!_pageActions.TryAdd(key.ToString(), interaction.InteractionAction))
                                ConsoleUIManager.LogError($"Cannot add duplicate keys to page's action list. Offending key {key.ToString()} already has an associated action on this page\nContinuing without adding");
                        }
                    }
                }
            }

            foreach (ConsoleKey key in page.GoBackInteraction.InteractionKey)
            {
                if (!_pageActions.TryAdd(key.ToString(), page.GoBackInteraction.InteractionAction))
                    ConsoleUIManager.LogError($"Cannot add duplicate keys to page's action list. Offending key {key.ToString()} already has an associated action on this page\nContinuing without adding");
            }
        }
        #endregion

        #region METHODS
        public bool RollBackToPreviousePage()
        {
            if (_pageStack.Count == 0)
                throw new System.InvalidOperationException("Must Initialize the instance and set the PageCollection!\nNo items in the PageCollection!");

            if (_pageStack.Count > 1)
            {
                lock (_setPageLock)
                {
                    _pageStack.RemoveAt(_pageStack.Count - 1);

                    _pageActions.Clear();

                    ConsolePage page = _pageStack.Last();

                    if (page.NavigationInteractions != null)
                    {
                        foreach (NavigationInteraction interaction in page.NavigationInteractions)
                        {
                            if (interaction.InteractionVisible.Invoke())
                            {
                                foreach (ConsoleKey key in interaction.InteractionKey)
                                {
                                    if (!_pageActions.TryAdd(key.ToString(), interaction.InteractionAction))
                                        ConsoleUIManager.LogError($"Cannot add duplicate keys to page's action list. Offending key {key.ToString()} already has an associated action on this page\nContinuing without adding");
                                }
                            }
                        }
                    }

                    foreach (ConsoleKey key in page.GoBackInteraction.InteractionKey)
                    {
                        if (!_pageActions.TryAdd(key.ToString(), page.GoBackInteraction.InteractionAction))
                            ConsoleUIManager.LogError($"Cannot add duplicate keys to page's action list. Offending key {key.ToString()} already has an associated action on this page\nContinuing without adding");
                    }

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the _currentPage to currentPage argument if its within the range of the collection
        /// </summary>
        /// <remarks>
        /// This should be used cautiously in an environment where multiple threads may call it. Prefer the overload with string param in such cases
        /// </remarks>
        /// <returns>true if set, false otherwise</returns>
        public bool SetCurrentPage(int newPageIndex)
        {
            lock (_setPageLock)
            {
                if (newPageIndex < _pages.Count && newPageIndex >= 0)
                {
                    _pageActions.Clear();

                    ConsolePage page = _pages[newPageIndex];

                    _pageStack.Add(page);

                    if (page.NavigationInteractions != null)
                    {
                        foreach (NavigationInteraction interaction in page.NavigationInteractions)
                        {
                            if (interaction.InteractionVisible.Invoke())
                            {
                                foreach (ConsoleKey key in interaction.InteractionKey)
                                {
                                    if (!_pageActions.TryAdd(key.ToString(), interaction.InteractionAction))
                                        ConsoleUIManager.LogError($"Cannot add duplicate keys to page's action list. Offending key {key.ToString()} already has an associated action on this page\nContinuing without adding");
                                }
                            }
                        }
                    }

                    foreach (ConsoleKey key in page.GoBackInteraction.InteractionKey)
                    {
                        if (!_pageActions.TryAdd(key.ToString(), page.GoBackInteraction.InteractionAction))
                            ConsoleUIManager.LogError($"Cannot add duplicate keys to page's action list. Offending key {key.ToString()} already has an associated action on this page\nContinuing without adding");
                    }

                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Sets the _currentPage to pageName argument if it exists
        /// </summary>
        /// <remarks>
        /// This is the prefered method when multiple threads may be trying to change the page
        /// </remarks>
        /// <returns>true if set, false otherwise</returns>
        public bool SetCurrentPage(string pageName)
        {
            lock (_setPageLock)
            {
                int newPageIndex = _pages.FindIndex(x => x.PageName == pageName);

                if (newPageIndex < _pages.Count && newPageIndex >= 0)
                {
                    _pageActions.Clear();

                    ConsolePage page = _pages[newPageIndex];

                    _pageStack.Add(page);

                    if (page.NavigationInteractions != null)
                    {
                        foreach (NavigationInteraction interaction in page.NavigationInteractions)
                        {
                            if (interaction.InteractionVisible.Invoke())
                            {
                                foreach (ConsoleKey key in interaction.InteractionKey)
                                {
                                    if (!_pageActions.TryAdd(key.ToString(), interaction.InteractionAction))
                                        ConsoleUIManager.LogError($"Cannot add duplicate keys to page's action list. Offending key {key.ToString()} already has an associated action on this page\nContinuing without adding");
                                }
                            }
                        }
                    }

                    foreach (ConsoleKey key in page.GoBackInteraction.InteractionKey)
                    {
                        if (!_pageActions.TryAdd(key.ToString(), page.GoBackInteraction.InteractionAction))
                            ConsoleUIManager.LogError($"Cannot add duplicate keys to page's action list. Offending key {key.ToString()} already has an associated action on this page\nContinuing without adding");
                    }

                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Used to query the List of ConsolePages to get an index
        /// </summary>
        /// <param name="pageName"></param>
        /// <returns>Return the index of the ConsolePage with the name pageName if one exists, -1 otherwise</returns>
        public int GetPageIndex(string pageName)
        {
            lock(_getPageIndexLock)
                return _pages.FindIndex(x => x.PageName == pageName);
        }
        #endregion
    }
}
