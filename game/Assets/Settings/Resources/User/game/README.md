For now, I'm submitting the user preferences to source control, because the terminal font and color and stuff are saved there.
I need to make a system of backup indirection.
Aka, the defaults need to be customizable and saveable, but this should also apply to the settings defined by the concrete user of the terminal, including either a developer or at runtime.
At the same time, there should be defaults defined in code, to be able to restore the defaults / append to them when new properties get added.