using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.BrowserActions.Helper;

namespace CefBrowserControl
{
    [Serializable]
    public abstract class BaseObject : InputParameters, IInstanciateInputParameters
    {
        public string TranslatePlaceholderStringToSingleString(string value)
        {
            foreach (var keyValuePair in OverloadData)
            {
                value = value.Replace(keyValuePair.Key, keyValuePair.Value);
            }
            return value;
        }

        public StringOrRegex TranslatePlaceholderStringOrRegexToSingleStringOrRegex(StringOrRegex value)
        {
            if (value == null)
                return null;

            List<string> placeHolderList = ExtractAllPlaceholdersFromString(value.Value.Value);
            for (int i = 0; i < placeHolderList.Count; i++)
            {
                foreach (var keyValuePair in OverloadData)
                {
                    if (keyValuePair.Key == placeHolderList[i])
                    {
                        value.Value.Value = value.Value.Value.Replace(placeHolderList[i], keyValuePair.Value);
                    }
                }
            }
            return value;
        }

        public static string ConvertStringListToPlaceholderString(List<string> values)
        {
            string returning = "";
            foreach (var value in values)
            {
                returning += ConvertStringToPlaceholderString(value);
            }
            return returning;
        }

        public static string ConvertStringToPlaceholderString(string input)
        {
            return Options.PlaceholderPre + input + Options.PlaceholderPost;
        }

        public static List<string> ExtractAllPlaceholdersFromString(string text)
        {
            List<string> readingValues = ExtractPlaceholdersToListWithoutOutputConcat(text);
            List<string> returnValues = new List<string>();
            for (int i = 0; i < readingValues.Count; i++)
            {
                if (readingValues[i] == "output")
                {
                    if (i + 2 > readingValues.Count)
                        ExceptionHandling.Handling.GetException("Unexpected",
                            new Exception("There are parameters missing..."));
                    returnValues.Add(ConvertStringToPlaceholderString(readingValues[i]) + ConvertStringToPlaceholderString(readingValues[i + 1]) + ConvertStringToPlaceholderString(readingValues[i + 2]));
                    i += 2;
                }
                else
                {
                    returnValues.Add(ConvertStringToPlaceholderString(readingValues[i]));
                }
            }
            return returnValues;
        }

        public static string ExtractSinglePlaceholderToString(string value)
        {
            string returningValue = "";
            int positionBeginning = 0, positionEnding = 0;
            positionBeginning = value.IndexOf(Options.PlaceholderPre, positionBeginning);
            positionEnding = value.IndexOf(Options.PlaceholderPost, positionEnding);
            if (positionEnding > positionBeginning)
            {
                positionBeginning += Options.PlaceholderPre.Length;
                returningValue = value.Substring(positionBeginning, positionEnding - positionBeginning);
            }
            return returningValue;
        }

        public static List<string> ExtractPlaceholdersToListWithoutOutputConcat(string value)
        {
            List<string> readingValues = new List<string>();
            int positionBeginning = 0, positionEnding = 0;
            while (true)
            {
                positionBeginning = value.IndexOf(Options.PlaceholderPre, positionBeginning);
                positionEnding = value.IndexOf(Options.PlaceholderPost, positionEnding);
                if (positionEnding <= positionBeginning)
                    return readingValues;
                positionBeginning += Options.PlaceholderPre.Length;
                readingValues.Add(value.Substring(positionBeginning, positionEnding - positionBeginning));
                positionEnding++;
                positionBeginning++;
            }
        }
        public bool ExecuteEventHandler { get; set; } = false;

        public void SetFinished(bool completed)
        {
            Completed = completed;
            if (ExecuteEventHandler)
                OnActionFinished(new EventArgs());
        }
        
        public event EventHandler ActionFinishedEventHandler;

        protected virtual void OnActionFinished(EventArgs e)
        {
            EventHandler handler = ActionFinishedEventHandler;
            handler?.Invoke(this, e);
        }

        public bool Completed;

        public bool Successful;

        public enum ActionState
        {
            Successfull,
            Failed,
        }

        public TimeSpan? Timeout { get; set; } = null;

        public int? TimeoutInSec = null;

        public bool TimedOut;

        public DateTime? FirstAccess { get; set; } = null;

        public List<KeyValuePairEx<string, string>> OverloadData = new List<KeyValuePairEx<string, string>>();

        //public List<KeyValuePairEx<ActionState, string>> ResultsList = new List<KeyValuePairEx<ActionState, string>>();

        public List<KeyValuePairEx<string, string>> ReturnedOutput = new List<KeyValuePairEx<string, string>>();

        //next 3 could be made static
        public List<string> ReturnedOutputKeysList = new List<string>();

        public string Description = "";

        public void NewInstance()
        {
            throw new NotImplementedException();
        }

        public  void ReadAvailableInputParameters()
        {
            throw new NotImplementedException();
        }
    }
}
