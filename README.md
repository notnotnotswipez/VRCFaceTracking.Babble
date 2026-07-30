# VRCFaceTracking.Babble
This is a modified version of the [Project Babble](https://github.com/Project-Babble/Baballonia) VRCFT module which combines the tobii gaze and blink data provided by [PSVR2Toolkit](https://github.com/BnuuySolutions/PSVR2Toolkit) with Babbles eye expressions (expressions beta branch).

Gaze data is entirely provided by Tobii with this module. Babble's own estimation for gaze will be ignored. Eye closing is read from Tobii data, but will use Babble's openness data when the eye is not considered closed by Tobii.

Ensure you have [VRCFaceTracking](https://store.steampowered.com/app/3329480/VRCFaceTracking/) installed and [Project Babble: Baballonia](https://store.steampowered.com/app/4091970/Project_Babble_Baballonia/) [Expressions Beta Branch] installed 

If you have installed the VRCFT-Babble module from VRCFaceTracking's module registry, uninstall that as it will conflict.

## Installation
1) Download the latest [release](https://github.com/notnotnotswipez/VRCFaceTracking.Babble/releases/latest).
2) Extract all the contents of the release zip into `AppData\Roaming\VRCFaceTracking\CustomLibs`.

When you run Baballonia and VRCFaceTracking, the module should start properly and you'll get tobii gaze and blink ontop of Baballonia expressions.
