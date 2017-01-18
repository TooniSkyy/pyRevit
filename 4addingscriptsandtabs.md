---
layout: page
title: Adding your own scripts
permalink: /howtoaddscriptsandtabs/
---

## Adding your own extensions:

pyRevit's extensions system has evolved again to be more flexible and easier to work with. We'll dive right into how you can add your own extension, but first let's answer one important question:

**Q:**
Why would I need to create a separate extension? Why Can't I just add my scripts to the current pyRevit tools?

**A:**
Because pyRevit is a git repository and the real benefit of that is that you can keep it always updated without the need to unsinstall and install the newer versions. To keep this system running without issues, I highly recommend to messing with the pyRevit git repository folders and contents and pyRevit makes it really easy to add your own extensions. You can even add tools to the standard pyRevit tab in your own extensions. I'll show you how.

Besides, by creating a separate extension, you'll have all your precious scripts and tools in a safe place and away from the changes being made to the core pyRevit. They can even live somewhere on your company shared drives and be shared between your teams.


### Contents:

- [Extensions](#extensions)
- [Bundles](#bundles)
- [Extension Bundles Structure](#extension-bundle-structure)
- [Library Extensions](#library-extensions)


&nbsp;


## Extensions
Each extension is a group of tools, organized in bundles to be easily accessible through the user interface.

Extensions are organized in a bundle with `.extension` postfix. Like this one:

![]({{ site.url }}/pyRevit/images/bundle.png)

There are two steps that you need to follow to create your own extensions:

- First, is to create a separate folder for all your custom extensions and tell pyRevit to load your extensions from this folder. This is done in the Settings window, under the Custom Extension folders section. This way your precious extensions will stay out of the pyRevit installation and are safe.

![]({{ site.url }}/pyRevit/images/customextfolder.png)

- Next, create your `<your extension name>.extension` folder under the custom extensions folder. Read the sections below on how to create bundles for your commands and the user interface.


&nbsp;


## Bundles:
A bundle is a folder named in this format:

`bundle_name.bundle_type`

Like these:

![]({{ site.url }}/pyRevit/images/bundle.png)

The most basic bundle is a command bundle. There are more than one type of command bundles but a `.pushbutton` bundle explained here covers %90 of the use cases. 

#### ![]({{ site.url }}/pyRevit/images/bundleicon.png) command_bundle.pushbutton
Here is the structure of a command bundle:

- Each command bundle needs to include a script either in python or C#:
	- `script.py`: is for python commands
	- `script.cs`: is for C# commands
- `icon.png`: Command bundles can include an icon for their user interface.
- `lib`: The can define a python library (a sub-folder named `lib` inside the bundle will do). This library will be accessible to the python script in this bundle. This organizes all the python modules that are necessary for this python script to work into one folder.

This is how a command bundle looks like:

![]({{ site.url }}/pyRevit/images/commandbundle.png)

And this is a more advanced command bundle with a configuration script and configuration window definition file:

![]({{ site.url }}/pyRevit/images/advancedcommandbundle.png)



&nbsp;


## Extension Bundle Structure:
Now that we have explained the command bundles, we need a way to organize these commands into a user-friendly interface. Let's introduce **Group Bundles**:

### Basics of Group Bundles:
A group bundle is a bundle that can contain command bundles and other group bundles. They come in all different shapes and sizes but they have a few features in common:

- They can contain command bundles and other group bundles. (But I've already said that)
- `icon.png`: Bundle can include an icon for their user interface.
- `lib`: The can define a python library (a sub-folder named `lib` inside the bundle will do). This library will be accessible to all the commands in this bundle and other child group bundles. This folder can contain all the python modules that are being shared between the child commands.
- `_layout`: This is a text file inside the bundle that defines the order in which the bundle contents should be created in the user interface. The contents of this file should be the names of the component in order.

Here is `_layout` file example. This is a layout file for a Group Bundle that has a series of push buttons and other group bundles under itself:

```
PushButton A
PushButton B
PullDown A
---
PullDown B
Stack3 A
>>>
PushButton C
PullDown C
```

Oh, and also:

- `---`: This line will add a separator to the interface
- `>>>`: Any bundle after this line will be created inside a slide-out. This works for panel bundles only.

And this is how a typical Group Bundle looks like:

![]({{ site.url }}/pyRevit/images/groupbundle1.png)


&nbsp;


Now let's talk about the different Group Bundles:


#### ![]({{ site.url }}/pyRevit/images/bundleicon.png) group_bundle.tab
This bundle creates a Tab in the Ribbon with the bundle name.

| **Example**    |      **Can Contain**             |
|:---------------|:---------------------------------|
| `pyRevit.tab`  |  Only `.panel` Group Bundles.    |


#### ![]({{ site.url }}/pyRevit/images/bundleicon.png) group_bundle.panel
This bundle creates a Panel in a Ribbon Tab with the bundle name.

| **Example**      |      **Can Contain**        |
|:-----------------|:----------------------------|
| `pyRevit.panel`  |  Any other bundle type      |

#### ![]({{ site.url }}/pyRevit/images/bundleicon.png) group_bundle.pulldown
This bundle creates a Pulldown Button in a Ribbon Panel or a Stack, with the bundle name and icon.

| **Example**         |      **Can Contain**        |
|:--------------------|:----------------------------|
| `pyRevit.pulldown`  |  Only command bundles       |


#### ![]({{ site.url }}/pyRevit/images/bundleicon.png) group_bundle.splitbutton
This bundle creates a Split Button button in a Ribbon Panel or a Stack, with the bundle name and icon.

| **Example**            |      **Can Contain**        |
|:-----------------------|:----------------------------|
| `pyRevit.splitbutton`  |  Only command bundles       |


#### ![]({{ site.url }}/pyRevit/images/bundleicon.png) group_bundle.splitpushbutton
This bundle creates a Split Push Button button (The sticky split button) in a Ribbon Panel or a Stack, with the bundle name and icon.

| **Example**                |      **Can Contain**        |
|:---------------------------|:----------------------------|
| `pyRevit.splitpushbutton`  |  Only command bundles       |


#### ![]({{ site.url }}/pyRevit/images/bundleicon.png) group_bundle.stack2
This bundle creates a stack of 2 buttons in a panel.

| **Example**                |      **Can Contain**        |
|:---------------------------|:----------------------------|
| `pyRevit.stack2`  |  Command bundles, `.pulldown`, `.splitbutton`, `.splitpushbutton`       |


#### ![]({{ site.url }}/pyRevit/images/bundleicon.png) group_bundle.stack3
Just like the `.stack2` bundle but with 3 buttons instead.

&nbsp;


## Advanced Bundles:
There are a few more advanced bundle types in pyRevit as well. Here is some quick intro on these bundles.

#### ![]({{ site.url }}/pyRevit/images/bundleicon.png) command_bundle.smartbutton
Smart buttons are python scripts that are written like modules. They should define `__selfinit__` function as shown below. This function gets executed at startup time to give a chance to the button to initialize itself (e.g set its icon based on its state).

```python
def __selfinit__(script_cmp, ui_button_cmp, __rvt__):
    """
    Args:
        script_cmp: script component that contains info on this script
        ui_button_cmp: this is the UI button component
        __rvt__: Revit UIApplication

    Returns:
    		bool: Return True if successful
    """

	run_self_initialization()
```


#### ![]({{ site.url }}/pyRevit/images/bundleicon.png) command_bundle.linkbutton
Link buttons can call a function from another Addin. To make a link button define the parameters below in the bundles `script.py`:

```python
__assembly__ = 'Addin assembly name'
__commandclass__ = 'Class name for the command'
```

**Note:** For this button to work properly, the target addin must be already loaded when this button is being created, otherwise Revit can not tie the UI button to an assembly that is not loaded.

For example to call the Interactive Python Shell from RevitPythonShell addin:

```python
__assembly__ = 'RevitPythonShell'
__commandclass__ = 'IronPythonConsoleCommand'
```


&nbsp;


## Library Extensions

Library extensions are created to share IronPython modules between all extensions. They're in essence IronPython module packages. Some users might decide to develop an IronPython library (e.g. [RevitPythonWrapper Library](http://revitpythonwrapper.readthedocs.io/)) that other users can use in their tools and benefit from.

Library extensions are identified by `.lib` postfix. The library extension folder address will be added to the `sys.path` of all the other extensions by the loader.

You can install the **RevitPythonWrapper** package (which is a library extension) through the pyRevit Extensions tool, and see how it works.



