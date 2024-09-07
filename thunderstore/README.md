# Nine Sols Modding API

A library mod containing utilities like
- utilities for working with unity objects, including integration with the red candle games lifecycle methods
- Toast messages using `ToastManager.Toast("message")`
- a `KeybindManager` for quick and easy keybindings
- various utilities for working with JSON and embedding files in the assembly
- Runtime Preloading of objects: When you want to reference an object from another scene, you can load the scene at startup, clone your object and unload the scene again.
- (in the future) mod configuration in the title screen

It also skips the starting animation and disable steam achievements while the mod is loaded.