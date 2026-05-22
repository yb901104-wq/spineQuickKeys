You can do this in 4.3. The launcher validates arguments before passing them to the edition, so you'll need to download a new launcher or use .--ignore-unknown-args

I apologize, but proper documentation for 4.3 is not complete. Here is the full 4.3 CLI usage:

 Editor: Spine [-hvlft] [-x <host:port>] [-u <version>] [<path>]
  Export: Spine [-i <path>] [-m] [-o <path>] -e <path>
          Spine [-i <path>] [-m] [-o <path>] -e json[+pack]|binary[+pack]
Skeleton import:
          Spine -i <path> [-s <scale>] -o <path>
          [--from <name>] [--to <name>] [--replace] -r
Skeleton merge:
          Spine -i <path> [-s <scale>] -o <path>
          [--from <name>] [--to <name>] --merge [--replace] -r
Animation import:
          Spine -i <path> [-s <scale>] -o <path>
          [--from <name>] [--to <name>] -a <name> [-a <name>]... [--replace] -r
Clean up: Spine -i <path> -m
    Pack: Spine -i <path> [-j <path>]... -o <path> -p <name>
          Spine -i <path> [-j <path>]... -o <path> [-n <name>] -p <path>
    Info: Spine -i <path>

Export JSON, binary, images, or video:
-i,--input   Path to a folder, project, or data file. Overrides export JSON.
-m,--clean   Animation clean up is performed before export.
-o,--output  Path to write export file(s). Overrides export JSON.
--set name=value  Override a settings JSON field. Repeatable.
-e,--export  Path to export settings JSON file.

Import skeletons or animations from a project or data file into a project:
-i,--input      Path to a folder, project, or data file to be imported.
-o,--output     Path to project file to import into. Created if nonexistent.
-s,--scale      Number or atlas file to scale the skeletons being imported.
--from          Source skeleton name. Optional if input has 1 skeleton.
--to            Destination skeleton name. Merges if exists, else creates.
-a,--animation  Animation name to import instead of skeleton. Repeatable.
--merge         Merge skeletons instead of importing as new.
--replace       Replace existing skeleton, attachments (merge), or animation.
-r,--import     Perform the import or merge.

Animation clean up:
-i,--input  Path to project file or folder.
-m,--clean  Animation clean up is performed and the project is saved.

Texture atlas packing:
-i,--input    Path to folder of images to be packed.
-o,--output   Path to write texture atlas and PNG files.
-j,--project  Path to a project to determine which images are used by meshes.
-n,--name     Texture atlas name, the prefix for the atlas and PNG files.
--set name=value  Override a pack settings field. Repeatable.
-p,--pack     Texture atlas name or path to pack settings JSON file.

Texture atlas unpacking:
-i,--input    Path to folder of atlas images.
-o,--output   Path to write unpacked image files.
-j,--project  Path to a project to determine which images are used by meshes.
-c,--unpack   Path to texture atlas file.

Project information:
-i, --input  Path to a folder, project, or data file.

Path patterns for -i and -j (include quotes):
"/path/**/*.spine"           Wildcard pattern.
"/path,1/*.spine,2/*.spine"  Root folder then comma separated patterns.
"/path,**/*.spine,!**/wip"   Prefix ! to exclude.
"/path,~.*[0-9]\.spine"      Prefix ~ for regex.
"/work,~.*\.spine,!~.*/wip"  Regex with exclude.

Advanced:
-Xmx8192m           Set the maximum memory usage in megabytes (4096 default).
--trace             Enable additional logging and diagnostic checks.
--auto-start        Start automatically.
--no-auto-start     Do not start automatically.
--ping              Test latency to each server (otherwise done every 4 days).
--server x          Set the preferred server regardless of ping (eg jp/us/eu).
--disable-audio     Disable all audio support.
--pretty-settings   Format settings files more nicely.
--keys              Enable hotkey popups by default.
--hide-license      Don't show name and email on launcher (eg for streaming).
--ui-scale x        Set the interface scale (eg 200).
--icc-profile x     Set the path to the ICC profile file for color management.
--intro             Show the Esoteric Software logo intro.
--clean-all         Animation clean up for all exports.
--animate-mode      Open projects in animate mode.
--no-save-prompt    Never prompt when closing an unsaved project.
--mesh-debug        Show debug information on top of meshes.
--export-selection  Editor selection is shown in image and video exports.
--reuse-instance    Reuse an existing Spine instance to open a project file.
--no-reuse-instance Don't reuse an existing Spine instance.
--skeleton-viewer   Run the Skeleton Viewer.
--ignore-unknown    Don't error if a CLI parameter is not recognized.

Examples:
Spine --export /path/to/export.json
Spine --export "/path/with spaces/to/export.json"
Spine --input /path/to/project.spine --output /path/to/output/
      --export /path/to/export.json
Spine -i /path/to/project.spine -o /path/to/output/ -e /path/to/export.json
Spine -i /path/to/project.spine -o /path/to/output/ -e binary+pack
Spine -e /path/to/export1.json -e /path/to/export2.json
Spine -i /path/to/images/ -o /path/to/output/ --pack /path/to/pack.json
Spine -i /path/to/images/ -o /path/to/output/ -n name -p /path/to/pack.json
Spine -i /path/to/project1.spine -o /path/to/output/ -e /path/to/export1.json
      -i /path/to/project2.spine -e /path/to/export2.json -i /path/to/images/
      -o /path/to/output/ -p /path/to/pack.json
Spine -i /path/to/skeleton.json -o /path/to/project.spine --to skeletonName -r
Spine -i /path/to/from.spine -o /path/to/to.spine --merge -r
Spine -i /path/to/from.spine -o /path/to/to.spine --to dst --merge --replace -r
Spine -i /path/to/from.spine -o /path/to/to.spine -a walk -a run --replace -r
Spine -i /path/to/project.spine -o /path/to/output/
      --set packAtlas.stripWhitespaceX=false -e json+pack
Spine -i "/projects/**/*.spine" -e binary+pack
Spine -i "/projects/**/*.spine,!**/wip/**" -m

I know that's a lot of information! You probably want:

--input input --output output.spine --animation animationName --import
Or the same using the short form parameters:

-i input -o output.spine -a animationName -r
Where is JSON, binary, or a file.input.spine

I notice that you can specify / multiple times, but there is no way to easily specify all animations. We'll make / with no name after it mean "all animations".-a--animation-a--animation

Also new in 4.3 is built-in globbing, that's the "Path patterns" section. It's very convenient! Not many people (maybe no one) have used it yet, so there may be some hiccups, but if you let us know we'll any problems quickly.