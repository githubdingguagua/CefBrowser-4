# CefBrowser (docs are outdated!)
## Howto Use:
 - Clone
 - Recover the NUGET packages
 - Clone dependencies(so that CefBrowser, ExceptionHandling, Encoding & HashingEx are in the same folder; vs studio integration purposes, do not change that) 
 e.g.:
 ```
./
../
CefBrowser/
EncodingEx/
ExceptionHandling/
HashingEx/
```

## Dependencies
 - ExceptionHandling
 - EncodingEx
 - HashingEx
 - RPC-Communication
 - SerializationDotNet2
  
 
Example folder structure:


## Technical
 - Running on CEF# 57.0.0
 - Running on Chromium 3.2987.1601
 - Needing following Nugget Packages
  - CefSharp.Wpf
  - CefSharp.Common
  - cef.redist.x64

## About
This own implementation let the user access the dom of a document with all of its frames. It basically implements a lot of javascript code, so the user hasn't to write it on his own.  
The whole Browser is splitted up in various parts:
 - API
 - Browser Action
 - Gateway
 - Browser
  
The API can be used to create numerous Browser Actions. These browser actions are sent(submitted) to the gateway, which takes care of the stacked browser actions. The Gateway runs in its own thread and executes the incomming browser actions as the come in (First Come, First Out). The gateway takes a action and does the talking with the browser

### Gateway
Runs a Timer, so it is independent from the main-thread
Should not used by the API! only use the browseractionmanager!  
After a action is finished, it sets the gateway action to completed, so the browseractionmanager knows that the other thread has finished it operations.  
It also can set a success parameter, if a javascript or anything else has properly executed
Supports the following types:
 - Frame Control
  - (GetParentFrameNames)Iterates up all parent frames for one frame
  - (FindElementsFrame) Get the corresponding frame, in which a dom element is located in
  - (EvaluateJavascript) Executes Javascript and returns the output into the gatewayaction which can be used by the browseraction
  - (ExecutesJavascript) Simply executes Javascript
  - (GetSize) returns the size of the actual browser
  - (IsBrowserInitiated) checks if the browser is ready
  - (SendKeyBoard) To send keystrokes or a simple key for emulating a users input
  - (SendMouseClick) sends a mouse click on a specified location(scrolling must be implemented)
  - (SendMouseWheel) for scrolling the webbrowser
  - (SiteLoaded) returns if the main site has fully loaded and
  - (ResourceLoaded) checks if a specfiic resource in a specific frame has completely finished loading all of its resources.
  - (FrameLoaded) returns if a frame has finished loading the main resources
  - (ShowMessageBox) pops a messagebox up
  - (GetFrameNames) gets a the framenames

Has following Actions visible for The BrowserActionManager:
 - AddGatewayAction(object action)

#### Thread safety for gatewayactions:
The Gateay enters a lock when adding an element and exits it after successfully adding it

The gatway enters a lock if it deqeues an gateway action and exits it afer successfully deqeuuing it.

#### Parameters
The Timeout can be set, so if a actions takes too long, the actiongateway could stop it(but it is not implemented yet)

#### Browser Hooks
The Gateway gets the list of the frames loaded by the browser. Every frames gets logged by the browser! If the url of an frame gets changes, it(the browser) changes its state and url as well.

#### Howto Call
Create one of the GatewayAction through the BrowserActionManager (from an BrowserAction). It must be anything but no BaseObject

#### TODO Describe parameters in each gateway action

### Browser Action Manager
This is the force, you wanna play with. It has it's own timer for thread independence(DO NOT REMOVE IT, unless you want to have some buggy browser) with its own locking procedure.  
It can be called with AddBrowserActions() and call events when got finished  
It starts when ActionsEnabled is set to true!
An action can fail or be successfull  
An action can be set with:
 - ElementsLoaded,
 - SecondsPassed,
 - ResourcesLoaded,
 - Immediately,
 - SiteLoaded,
 - FrameLoaded,
 - AttributeSetTo,
 - StyleSetTo,
 - ExecuteJavascript,
 - TriggerEvent,
 - Wait,
 - ReturnNodes,
 - Nothing,
 - TypeTextIn,
 - GetAttribute,
 - GetStyle,
 - SetAttribute,
 - SetStyle,
 - ClickOnElement,
 - GetFrameNames,

An action can be executed on following javascript things:
 - Id,
 - Name,
 - TagName,
 - Class,
 - Xpath,

It should be executed on a specific frame using the ActionFrameName variable, which can be a string or regex  
It must be set to finished with SetFinished to call the attached eventhandler properly  
The Action can use a selector with the executeon (id, name...) and can pre-define how many elements are going to be expected(mass change of things is possible :))
It also can have a timeout

The browser action manager handles lists of browseractions. But, a single browseraction can be passed to the browseractionmanager, it will convert it to a list anyway. So it is possible to chain various actions together.

In later usage, the browseractionmanager chould also get all dimensions and locations of any element in a browsers window.
