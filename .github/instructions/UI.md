# UI Design Rules for Coding Agents

## Prime Directive

A UI is good when the program behaves the way the user expected it to behave.

Optimize for user control, predictability, and task completion. Every surprise, hesitation, unnecessary decision, or unexplained behavior makes the product feel worse, even when the underlying feature technically works.

## Design Priorities

1. **Match the user’s mental model.**

   * Prefer behavior users already expect from similar apps, platforms, and controls.
   * When the program model conflicts with the user model, change the program model whenever possible.
   * Do not rely on manuals, tooltips, explanations, or warning dialogs to teach a surprising model.

2. **Design around activities, not feature lists.**

   * Start with what the user is trying to accomplish.
   * Organize screens, flows, and navigation around common tasks.
   * Features that do not support a core activity should be delayed, hidden, or removed.
   * Prefer “What do you want to do?” flows over blank-canvas feature dumps when users need guidance.

3. **Reduce decisions.**

   * Every option asks the user to stop and think.
   * Do not expose preferences just because the team cannot decide.
   * Make good defaults.
   * Add options only when the choice is central to the user’s task or harmless personalization.
   * Avoid settings that can accidentally put the UI into a confusing state.

4. **Be consistent.**

   * Follow platform conventions unless there is a strong user-centered reason not to.
   * Use standard controls, shortcuts, labels, and behaviors.
   * Do not reinvent common controls for visual novelty.
   * Creative styling is fine only when it does not break expected behavior.

5. **Make actions visually obvious.**

   * Interactive elements must look interactive.
   * Buttons should look clickable.
   * Drag handles should look draggable.
   * Tabs should clearly show selected state and available alternatives.
   * Do not make clickable and non-clickable elements look the same.

6. **Respect limited attention.**

   * Assume users are distracted, impatient, and trying to finish another task.
   * Do not require careful reading to operate the UI.
   * Keep labels and dialogs short.
   * Remove polite filler, lectures, implementation details, and “are you sure?” anxiety unless truly needed.

7. **Respect imperfect motor control.**

   * Use large click targets.
   * Allow clicking the whole control, not just tiny arrows or icons.
   * Avoid interactions requiring pixel-perfect mouse movement.
   * Avoid tiny scroll regions.
   * Snap objects to likely useful positions when appropriate.
   * Make common actions possible with keyboard as well as pointer.

8. **Do not make users remember what the computer can show.**

   * Prefer recognition over recall.
   * Use menus, lists, previews, thumbnails, recent items, autocomplete, and sensible suggestions.
   * Preserve context and previously entered information.
   * Suggestions must be easy to accept, ignore, or overwrite.

## Do

* Identify the top user activities before designing screens.
* Sketch flows around tasks, not around database objects or internal modules.
* Prefer simple models users can guess.
* Prefer familiar conventions over clever inventions.
* Use standard platform controls where possible.
* Support common shortcuts users will try.
* Make default behavior safe and unsurprising.
* Make destructive actions reversible where possible.
* Confirm only actions that are destructive, expensive, or hard to undo.
* Use concise, plain language.
* Put important actions where users expect them.
* Make primary actions visually distinct.
* Make secondary actions available but not distracting.
* Make disabled/unavailable states understandable.
* Make errors actionable: say what happened and how to fix it.
* Preserve user work aggressively.
* Let users recover from mistakes.
* Optimize the first-run path for immediate success.
* Optimize repeated use for speed.
* Make the UI usable without reading documentation.
* Make the UI usable under partial attention.
* Make the UI usable with imprecise pointing devices.

## Don’t

* Do not design from an internal feature checklist.
* Do not expose implementation details as UI choices.
* Do not ask users questions they do not care about.
* Do not add preferences as a substitute for design decisions.
* Do not assume users read manuals.
* Do not assume users read dialog text.
* Do not write long instructional dialogs.
* Do not use confirmations for harmless actions.
* Do not punish users for clicking the wrong thing.
* Do not require precise mouse control.
* Do not hide important actions behind tiny targets.
* Do not make users scroll tiny dropdowns when there is room to show more.
* Do not use custom controls that break expected platform behavior.
* Do not change standard shortcuts for ideological reasons.
* Do not make clickable text look like static text.
* Do not make static decoration look clickable.
* Do not invent metaphors that do not actually explain the behavior.
* Do not rely on “advanced users can customize it.”
* Do not assume advanced users want configuration more than predictability.
* Do not optimize for showing off design creativity at the cost of usability.
* Do not make users remember names, codes, commands, or locations that can be shown.
* Do not require users to understand your data model before they can succeed.

## Low-Level UI Rules

### Buttons and Actions

* Use clear verb labels: `Save`, `Delete`, `Send`, `Export`.
* Prefer specific labels over generic ones: `Delete Project` beats `OK`.
* Put the primary action in the expected platform position.
* Make dangerous actions visually and spatially distinct.
* Do not use `OK` when the action can be named.
* Do not show confirmation dialogs for safe, reversible, or expected actions.
* For destructive actions, explain the consequence briefly and offer a clear cancel path.

### Forms

* Ask only for information needed now.
* Use defaults whenever the likely answer is known.
* Preserve entered data after validation errors.
* Put validation messages next to the relevant field.
* Use forgiving input formats where possible.
* Avoid making users type exact strings that could be selected from a list.
* Use autocomplete when prior entries or likely values exist.
* Make editable text legible; prioritize clarity over visual delicacy.

### Dropdowns and Lists

* Do not make dropdowns unnecessarily short.
* Show as many options as practical.
* Let users click the whole dropdown, not only the arrow.
* For long lists, provide search, autocomplete, grouping, or filtering.
* Prefer thumbnails/previews when visual recognition is easier than reading names.

### Navigation

* Organize navigation by user goals.
* Keep common tasks obvious from the first screen.
* Make current location/state visible.
* Use tabs when switching between peer sections.
* Do not hide core workflows in menus users must explore blindly.
* Provide a clear path back and a clear path forward.

### Text and Copy

* Shorten aggressively.
* Use plain words.
* Remove filler like “please,” “note that,” and implementation explanations unless they help the task.
* Put the most important words first.
* Avoid paragraphs in dialogs.
* Do not explain what the system is doing unless the user needs that information to decide or recover.

### Errors

* Errors should help users regain control.
* Say what failed, why if known, and what the user can do next.
* Do not blame the user.
* Do not expose raw technical errors unless the user is technical and the detail is useful.
* Keep the original task recoverable.

### Customization

* Good: choices that affect the user’s work product or harmless appearance.
* Bad: choices about internal behavior, storage, indexing, layout mechanics, or other things users do not care about.
* Avoid customization that can accidentally break the interface.
* If customization exists, provide reset/recover behavior.

### Metaphors and Affordances

* Use metaphors only when they make behavior easier to guess.
* Prefer obvious affordances over decorative minimalism.
* Make controls communicate how they are used.
* If users must be taught the metaphor, the metaphor is weak.
* A bad metaphor is worse than no metaphor.

## Usability Review Checklist

Before shipping UI, verify:

* Can a new user tell what this screen is for?
* Can the user start the main task without reading documentation?
* Does the UI behave like similar apps/platforms?
* Are the most common activities easiest to perform?
* Are any options present only because the designer avoided choosing?
* Are all choices meaningful to the user’s task?
* Are click targets large enough?
* Can the UI survive misclicks and imprecise pointer movement?
* Can the user recover from mistakes?
* Are dialogs short enough to be read at a glance?
* Are confirmations reserved for destructive or hard-to-undo actions?
* Are standard controls and shortcuts preserved?
* Are clickable things visibly clickable?
* Are non-clickable things visually non-clickable?
* Does the UI show information the user would otherwise need to remember?
* Have at least a few real people tried the flow without coaching?
* Did observers watch what users did instead of only listening to what they said?

## Agent Instruction

When implementing UI, do not merely satisfy the functional requirement. Choose the design that makes the user feel in control, minimizes surprise, minimizes reading, minimizes precision work, minimizes memory burden, and best matches the conventions users already know.
