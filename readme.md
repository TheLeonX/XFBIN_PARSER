# Requirements
Make sure you installed that!
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.407-windows-x64-installer

# How to use parser?
1. Drag and Drop on exe.
2. Right click on file/folder.
3. Write path to file/folder in cmd.

# How to remove buttons from context menu

You can use batch script "**Uninstall XFBIN Parser.bat**" inside of archive or you can edit registry manually and delete this folders from registry:
- HKEY_CLASSES_ROOT\\.xfbin
- HKEY_CLASSES_ROOT\\XFBIN
- HKEY_CLASSES_ROOT\\Folder\\shell\\XFBIN_PARSER

To edit registry manually, press on Win+R and write in command line "regedit".
