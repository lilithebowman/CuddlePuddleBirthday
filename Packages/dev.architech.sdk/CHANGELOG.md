# ArchiTech SDK Changelog
All notable changes to this project will be documented in this file

Structure used for this document:
```
## Version Number (Publish Date)
### Added
### Changed
### Deprecated
### Removed
### Fixed
```

<!-- CHANGELOG -->

## 0.22.0 (2025-10-15)
### Changed
- Improve correctness in handling reference type, including arrays, serialized property extension methods.

## 0.21.3 (2025-08-15)
### Changed
- Limit property drawer scope to improve performance in scenes with many game objects.

## 0.21.2 (2025-07-06)
### Fixed
- Handling array assignment for SetVariableByName.
- Prevent object reference error log on exit.

## 0.21.1 (2025-05-27)
### Fixed
- Fix bad order of operations in combined null fallback ternary logic.

## 0.21.0 (2025-05-23)
### Added
- Add checkpoint time tracking API to inspector for debugging how long an inspector takes to render.

### Fixed
- Fix incorrect type accessing for getOptions when property is an array.

## 0.20.0 (2025-05-14)
### Added
- Add various method overloads for DrawVariable* calls that allows passing in an explicit serialized object reference.
- Add SetVariableByName overloads to pass in a custom serialized object reference.
- Add GetOrAddComponent overloads for returning whether the component is brand new or not.
- Add GetValue, GetValueType and SetValue overloads for passing in a serialized object reference.

## 0.19.0 (2025-03-15)
### Added
- Add EditorPrefs-like class which stores preferences on a per-project basis. `ATProjectPrefs`
- Fix an uncommon null pointer exception.

## 0.18.1 (2024-12-10)
### Changed
- Move GetLabelWidth from nested in ShrinkLabelScope to directly on the ATEditorGUIUtility class.

## 0.18.0 (2024-12-05)
### Added
- Add MoveComponentToIndex to shift component to specific position as close as possible.
- Add ATPropertyWithDropdownDrawer to consolidate the component dropdown inspector logic.
- Add PropertyDropdownScope for handling the explicit enable/disable of the component dropdown element.
- Add GetOrAddComponent helper overloads for simplifying enforcement of the existence of a component.

### Changed
- Simply base editor class logic for handing the new dropdown property drawer logic.
- Heredoc cleanup.

### Removed
- Remove DrawVariableWithDropdown<T> generic overloads.
  - Handled implicitly with the new property drawer.

## 0.17.1 (2024-09-25)
### Fixed
- Array out of bounds issue for ATReorderableList

## 0.17.0 (2024-09-25)
### Added
- Add context menu handling from ATFoldoutArrayTuple into parent class ATMultiPropertyList.
- Add foldout toggle from FoldoutArrayTuple into ATReorderableList to allow visually shrinking the list.
- Add condensed layout for boolean properties of an ATReorderableList.
- Add method overload for DrawFoldoutForToggle which returns an out parameter of the actual property's value.

### Changed
- Add layout logic to ensure that any property hints are above a property.
- Improve ATReorderableList constructors.
- Change default U# program header to a more condensed version.
- Update default array property draw from ATFoldoutArrayTuple to ATReorderableList.

### Deprecated
- Deprecate ATFoldoutArrayTuple in favor of ATReorderableList.

## 0.16.4 (2024-07-10)
### Fixed
- Regression issue causing compilation failure on unity 2019.

## 0.16.3 (2024-06-29)
### Fixed
- Drawing variables with a dropdown selector should work again.

## 0.16.2 (2024-06-17)
### Fixed
- Array index out of bounds fix, whoops. 

## 0.16.1 (2024-06-17)
### Changed
- Updating of the log level on an event manager will update respective listeners log level is override is enabled.

## 0.16.0 (2024-06-13)
### Added
- Add base editor methods for drawing a variable dropdown that doesn't require explicitly providing array parameters.
- Add a template error helpbox method to base behaviour editor.
- Add SerializedProperty extension method for setting the value of an array property as a full new array.
- Add SerializedProperty extension method for getting the actual class type of the property.
- Expose the header rect for ATReorderableList.
- Add support for sending managed events with delayed frames or seconds to listeners.
- Add tooltip inclusion support for the I18n TrContent overloads.

### Changed
- Rename `DrawVariableDropdown` to `DrawVariableWithDropdown`.

### Fixed
- Fix for situations where the internal object count doesn't match the main property's length.

## 0.15.6 (2024-04-20)
### Fixed
- Regression in the event handler. Multi-depth event calling should be working again.

## 0.15.5 (2024-04-14)
### Added
- Add support for the convenience dropdown for UdonLogger type.

### Changed
- Move obsolete classes into Deprecated folder to avoid confusion.

### Fixed
- Fix inspector bug that was unexpectedly clearing some arrays.
- Fix edge-case where data save would be repeatedly called when ForceSave was called.

## 0.15.4 (2024-03-15)
### Added
- Add constructor overload to provide an optional ATBaseEditor reference.
- Add new SectionScope GUI.Scope type for handling a boxed section with a title.
- Add ATEditorGUILayout methods for handling common Header drawing.

### Changed
- Update ATReorderableList to implicitly mark variables involved in the list as drawn if an editor is provided in the constructor.

### Fixed
- Enable recursion to fix issues when calling a method that triggers a listener event while already in a listener event.
- Add explicit event use for delete key to prevent the default behaviour as the custom delete needs to be handled differently.
- Add null check for main property in MultiPropertyList to avoid a couple edge-cases.

## 0.15.3 (2024-02-23)
### Changed
- Adjust component context menu ordering.
- Update base editor property setting methods to use SerializedProperty extension methods.
- Improve stability of the ATReorderableList when manipulating entries.
- Switch to new Select method on ReorderableList for entry focus for 2022 and later.
- Remove old reflection-based index selection.

## 0.15.2 (2024-01-27)
### Added
- Add isPC and isIOS flags to the behaviour base class.
- Add methods for logging content without the formatted prefixes.

## 0.15.1 (2024-01-11)
### Added
- Component helper method for getting a list of distinct names along with the components.

### Fixed
- Fix 2019 compatability.
- Mitigation for edge-case where certain editor text will be the incorrect color.

## 0.15.0 (2024-01-02)
### Changed
- Update incorrect namespace for collider editors.
- Include missing AddArrayProperty overrides for ATReorderableList.
- Expose ATReorderableList's header as a public field.
- Remove underscore prefix for inheritable protected methods in ATEventHandler.
- Update DrawElementDelegate to provide the list instance instead of the Properties and Labels.
- Update defaultDrawElement to changed delegate API.

### Fixed
- Include array size check in-case a removal happens out of bounds.
- Include throw when AddArrayProperty is given a null property.

## 0.14.1 (2023-12-16)
### Changed
- Minor corrections for 2022.
- Include trace log for variable values.
- Skip dropdown rendering if no options are provided.

## 0.14.0 (2023-11-26)
### Added
- New draw method which returns both a bool if it was modified as well as the current value of the property.
- New draw method for presenting a dropdown to the right of the property field which presents a list of known values that can be assigned to the field.
- New SerializedObject extension method for finding a property that will simply return false if the property is not found instead of throwing.
- Add base editor methods for drawing serializedproperties with a single label.
- Add support to `MultiPropertyLists` for visually hiding/showing a given property from the rendering.
  - Can be triggered via `HideProperties(params int[])` and `ShowProperties(params int[])`
  - You must pass in the properties indexes you wish to hide. The indexes are determined by the order in which they were called via `AddArrayProperty`

### Changed
- Better support for unity 2022 changes.
- `HandleSave` check now only runs `ApplyModifiedProperties` when the gui state has been explicitly modified.
  - This prevents duplicate calls when a property was updated without user input though automated means.
- Move `SaveObjectScope` to `ATEditorGUIUtility` for use outside of inherited inspector editors.

## 0.13.0 (2023-10-31)
### Added
- Add SerializedProperty helper extension methods `GetValue()`, `IndexOf(object)`, `Contains(object)`, `ResizeAndFill(int, object)`, `SetValue(int, object)`.
- Add option to ShrinkWrapLabelScope to specify if the shrink should apply to the label or the field itself.
- Add MultiPropertyList overload method `AppendNewEntry(object)` for adding a new entry with a given MainProperty object.
- Add MultiPropertyList method overload `Resize()` for forcing a resize to the main property's size.
- Add MultiPropertyList method `RemoveEntryByValue(object)` for removing an entry that contains the provided object.
- Add field to PropertyListData to store a related object reference.
- Add `ATEventHandler` class which is a functional merge of `ATEventManager` and `ATEventListener`.

### Changed
- Modify PropertyListData fields for more correct naming conventions.

### Deprecated
- Deprecate `ATEventManager` and `ATEventListener` in favor of `ATEventHandler`.

## 0.12.0 (2023-10-15)
### Added
- Add support for defining a default value for new array entries for MultiPropertyLists.
- Add resizing and resetting to MultiPropertyLists.
- Add FillValue and SetValue extensions for SerializedProperty.

### Changed
- Update event listener priority property to public.
- Make default layout draw use custom field width when type is boolean to reduce space used.

## 0.11.0 (2023-10-09)
### Added
- Add base method for getting a value by property or name.
- Add header draw methods for changing the font size of the header.
- Add ATEditorUtility overloads for non-generics usage of some methods.
- Add method overloads for getting a persistent UI event listener.
- Add event listener methods for starting/stopping listening and for modifying the registered event manager during runtime.

### Changed
- Update VRCSDK minimum to 3.4.0.

## 0.10.2 (2023-09-21)
### Changed
- Update the dependencies in preparation for VRChat's 3.4.0 merge update.

## 0.10.1 (2023-09-16)
### Fixed
- EventManager logic when updating the priority of a listener to HIGH should now assign the listener slot correctly instead of erroneously being off-by-one.

## 0.10.0 (2023-08-23)
### Added
- New base editor class for handling non-U# behaviours.
- Scripting defines for third-party integrations.
- Helper methods for getting parent components.
- Add UI line in editors below the base editor elements.

### Changed
- Rename ATUtils to ATUtility for naming consistency.

### Fixed
- Scripting defines helpers are more reliable.

## 0.9.3 (2023-08-13)
### Changed
- Update base editor behaviour to implicitly use the ATFoldoutArrayTuple for array properties that are being drawn implicitly.

### Fixed
- Fix ATFoldoutArrayTuple header adding extraneous entry count suffixes each draw while collapsed.

## 0.9.2 (2023-08-12)
### Added
- Scripting define utility methods.

### Changed
- ATMultiPropertyList DrawLayout now returns a boolean of whether or not the draw has detected any property changes.
  - This can be used in the custom editors to do additional actions.

### Fixed
- FoldoutArrayTuple no longer has the weird half-pixel vertical offset when the foldout is collapsed so it aligns correctly.
- Add a reflection checks to help mitigate the "did you call BeginChangeCheck first" error log under certain scenarios.

## 0.9.1 (2023-08-12)
### Added
- Generic utility methods originally in the protv repo.

## 0.9.0 (2023-08-07)
### Added
- Add foldout array dropzone visual.
- Add validation delegate for determining if the dropzone is valid for the dragged objects.

### Changed
- Remove dropped object from property struct and added the parameter to the respective delegate.
- Adjust foldout array dropzone area.

### Fixed
- Fix issue where sometimes deleting a PropertyList entry might crash unity... somehow.

## 0.8.1 (2023-07-26)
### Added
- Add support for context menu on the foldout array tuple header.
- Add support for bold text when prefabOverride is detected.

### Changed
- Adjust the default styling for the showHints boxes.
- Reduce indent level for generic foldout headers.

## 0.8.0 (2023-07-22)
### Added
- Add custom type for handling a tuple of array properties with a foldout setup.
- Add custom Foldout for boolean property types.
- Add custom Foldout that uses the generic isExpanded value.

### Changed
- Move EditorGUI related function into their own container classes and subfolder.
- Move custom editor helper types into their own subfolder.
- Cleanup foldout indentation handling.
- Cache reflection information.
- Update ATReorderableList to use the new ATMultiPropertyList structure.

## 0.7.9 (2023-07-17)
### Fixed
- Corrected type casting for ints in UpdateVariableByProperty.

## 0.7.8 (2023-01-13)
### Added
- Added missing GetPropertyLabel overload.

## 0.7.7 (2023-07-13)
### Added
- New `ActiveEvent` getter which returns the event name that is currently executing.
  - When no event is running, it returns null.

## 0.7.6 (2023-07-06)
### Added
- Add virtual methods for notifying child classes when a certain listener action occurs.

### Changed
- Update listener methods to make proper use of method overloading.
- Split variable update into two methods for additional usages.

### Fixed
- UdonLogger field should no longer be erroneously drawn when the dependency is not present.

## 0.7.5 (2023-07-01)
### Fixed
- Update collider editor overrides to respect local rotation as well as the local/global pivot tool option.

## 0.7.4 (2023-07-01)
### Changed
- Update IsOwner property to be public instead of protected.

## 0.7.3 (2023-06-27)
### Changed
- Optimize array resizing logic.

## 0.7.2 (2023-06-25)
### Added
- New helper method for explicitly removing a desired event from some Selectable's UIEvents list if it exists.
  - It will find ALL instances of the exact event and remove them.
  - All other events on that Selectable remain in-tact.

## 0.7.1 (2023-06-16)
### Changed
- Removed extraneous using statement.

## 0.7.0 (2023-06-14)
### Added
- Optional support for VUdon Logger is now built into ATBehaviour and ATEventManager.
- New Header method for the editor base class.
  - This replaces the PreChangeCheck method, which is now deprecated.
- New Footer method for the editor base class.
  - This replaces the PostChangeCheck method, which is now deprecated.

### Deprecated
- PreChangeCheck and PostChangeCheck, in favor of the updated Header and Footer methods.

## 0.6.0 (2023-06-01)
### Added
- New wrapper type for improved handling of ReorderableList.

### Changed
- Moved extension methods into their own specific class.

## 0.5.0 (2023-05-24)
### Added
- New abstract editor window base class

## 0.4.9 (2023-05-16)
### Fixed
- Move handles on Box/Sphere/Capsule colliders should properly respect the object's scale.

## 0.4.8 (2023-05-12)
### Added
- New helper method for updating a variable via SerializedProperty in an inspector.
- New Draw method for rendering a boolean in a foldout style.

## 0.4.7 (2023-05-06)
### Added
- Add new DrawVariableByNameWithLabel method to provide a custom GUIContent reference.
  - NOTE: The label is only applied to the first variable in the list, the rest implicitly use GUIContent.none.
- Add shorthand method for making a disabled group and a auto-orienting spacer that consumes size of only one dimension.

### Changed
- Modify the static attribute methods in ATEditorUtils to be extension methods.
- Modify GetPropertyAttribute(s) to respective GetAttribute(s) and GetFieldAttribute(s) methods for more descriptive naming.

## 0.4.6 (2023-04-18)
### Added
- Overloads to the DrawVariables* methods that take multiple property names to also take GUILayoutOptions.

### Fixed
- Provided properties order should now be respected for the DrawVariables* methods when multiple properties are provided.
  - This fixes the property draw order not matching up with the defined order in the editor script.

## 0.4.5 (2023-04-17)
### Added
- Add convenience scope class for handling customized editor gui field/label widths.

## 0.4.4 (2023-04-09)
### Added
- VPM deployment flow for easy VCC integration.

### Deprecated
- `isQuest` has been deprecated in favor of the more generic `isAndroid` value due to the upcoming VRC Phone version.

## 0.4.3 (2023-04-06)
### Changed
- Move the component move context menu logic into a utilities class.
- Modify a parameter for naming clarity.
- Update the static GetPropertyAttribute(s) methods in ATEditorUtils to be extension methods style, updated references as needed.
- Modify GetPropertyAttribute(s) to respective GetAttribute(s) and GetFieldAttribute(s) methods for more descriptive naming.
- Add shorthand method for making a disabled group and a auto-orienting spacer that consumes size of only one dimension instead of two like the default Unity Space call does.

## 0.4.2 (2023-01-30)
### Added
- Add option for alternative inline header style.
- Add context menu options for moving a component to either the top or bottom of a game object.

### Changed
- Modify the internal handling of max log level for simplified implementation.

## 0.4.1 (2023-01-23)
### Added
- Add DrawVariablesByNameAsObjectType and similar convenience methods to make it easier to customize how certain elements are drawn.
- Move the save logic in ATBehaviourEditor to a separate protected method to make it easier to customize.
- Move baseline inspector drawing logic to a separate protected method to make it easier to customize.
- Add assembly flag so editor scripts can access runtime internals.

## 0.4.0 (2022-12-30)
### Added
- Add utility method for fetching the index of a persistent listener including the argument object.
- Add editor method `DrawVariablesByNameWithoutLabels` for drawing variables explicitly without the labels.
- Add `Owner` property for conveniently getting and setting the current object's owner.
- Add experimental `ATReliableSync` class, further stress-testing needed.

### Changed
- Update the casing of the `IsOwner`, `IsMaster`, `IsInstanceOwner`, `EventManager` and `Priority` properties.


## 0.3.0 (2022-11-06)
### Added
- Add method `VariablesDrawn` to manually declare a variable as having been drawn.
- Add `DrawVariablesByType` method
- Add extension methods to Enum for getting a given attribute type.
- Add additional forced flag to check for explicit saving even if the GUI.changed stack doesn't match when the method is called.

### Changed
- Package name changed to `dev.architech.sdk`
- Add internal tracking of which variables have been drawn implicitly
- Update DrawVariables to only draw those which have yet to be drawn implicitly.
- Move `DrawVariables` to be called after `RenderChangeCheck`.
- Change `DrawVariables(string[])` to `DrawVariablesByName(string[])` for explicit clarity.
- Update DrawVariables* methods to correctly use the prop.propertyPath instead of prop.name
- Update array methods proxy to explicitly handle the type parameter.


## 0.2.1 (2022-10-14)
### Added
- Add one-time call InitData equivalent to OnEnable but occurs after LoadData.
    - Can be re-triggered by setting `init` to false.

### Changed
- Allow custom headers to be drawn with GUIContent as well as string.
- Change DrawVariables methods to return a bool for if any of the variables were modified.


## 0.2.0 (2022-10-05)
## Changed
- Add boilerplate translation method.


## 0.1.3 (2022-09-28)
## Changed
- Removed the log level from the log outputs of Errors and Warns since Unity implicitly marks those respectively


## 0.1.2 (2022-09-28)
### Added
- New property fields for specifying if a subclass should draw a default option.
    - `autoDrawHeader` for the default UdonSharp header fields
    - `audoDrawVariables` for rendering all serialized variables in a generic manner
- Default custom editor for EventManager types
    - Basically duplicates the the ATBehaviourEditor logic, but adds in the logLevelOverride field
- Implementation classes to act as default custom editors for the respective abstract classes

### Changed
- Moved default header rendering to after LoadData to allow for custom inspector headers to be rendered by subclasses.


## 0.1.1 (2022-09-28)
### Added
- Changelog file

### Changed
- Moved the change check logic for the logging level in the base editor class next to the change check logic for the
  derived classes.


## 0.1.0 (2022-09-26)
### Added
- Base class with consistent logging structure setup
- Abstract sub-classes for event management (pub/sub style)
- Abstract editor class for the base ATBehaivour class
- Custom utils class
