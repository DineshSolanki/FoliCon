---
switcher-label: IDE
---
# Translations

FoliCon is currently available in the following languages:

1. English
2. Hindi
3. Spanish
4. Arabic
5. Russian
6. Portuguese

FoliCon uses [HandyControls](https://ghost1372.github.io/handycontrol/langs/#Dynamic-Multi-Language) way of Resource files to store the strings for each language.

The resource files are located in the <ui-path>FoliCon/Properties/Langs</ui-path> folder,
and are named <path>Lang.[language-code].resx</path>. 

For example, the Hindi resource file is named <ui-path>Lang.hi.resx</ui-path>.

## Adding a new language from IDE {switcher-key="Visual Studio" collapsible="true" default-state="expanded"}

1. Right-click on the <path>FoliCon/Properties/Langs</path> folder and select <control>Add > New Item...</control>
   
    <img src="add-new-vs.png" alt="Create Resource file" style="block"/>
2. Select <control>Resource File</control> and name it `Lang.[language-code].resx`

    <img src="add-new-vs.png" alt="Select Resource file" style="block"/>
3. Open the newly created resource file and then:
   * Open the `Lang.resx` file and copy all the strings from there to the new resource file,
    
    <img src="lang-vs.png" alt="Copy strings" style="block"/>
4. Translate the strings in the new resource file.
5. Open the <path>Languages.cs</path> enum [file](https://github.com/DineshSolanki/FoliCon/blob/master/FoliCon/Models/Enums/Languages.cs) from <path>/Models/Enums/Languages.cs</path> and add the new language to the enum.
   
   <img src="lang-enum-vs.png" alt="Languages enum file"/>
6. Open the <path>CultureUtils.cs</path> [file](https://github.com/DineshSolanki/FoliCon/blob/master/FoliCon/Modules/utils/CultureUtils.cs) from <path>/Modules/utils/CultureUtils.cs</path>
   * Add the new Language enum and the language code to the `GetCultureInfoByLanguage` method.
   
    <img src="lang-culture-vs.png" alt="CultureUtils file"/>

## Adding a new language from IDE {switcher-key="Rider" id="adding-a-new-language_1" collapsible="true" default-state="expanded"}

1. Create a new resource file by either of the following methods:
   1. <control>Right-click</control> on the <path>FoliCon/Properties/Langs</path> folder and select <ui-path>Add > Resources (.resx)</ui-path>>
   
        <img src="add-new-rider.png" alt="Create Resource file" style="block"/>
   2. Open any existing resource file and then:
      1. Click on the <control>Add new culture (+)</control> button in the top-right corner of the resource file window.
        
          <img src="add-new-culture-rider.png" alt="Add new culture" style="block"/>
      2. Provide culture tag and click on `Add`.
      
          <img src="add-new-culture-rider-2.png" alt="Add new culture tag" style="block"/>
2. Now open any of the resource files, and you will see the missing strings for the new language.
3. Add translations.
4. Open the <path>Languages.cs</path>> enum [file](https://github.com/DineshSolanki/FoliCon/blob/master/FoliCon/Models/Enums/Languages.cs) from <path>/Models/Enums/Languages.cs</path> and add the new language to the enum.

   <img src="lang-enum-rider.png" alt="Languages enum file"/>
5. Open the <path>CultureUtils.cs</path> [file](https://github.com/DineshSolanki/FoliCon/blob/master/FoliCon/Modules/utils/CultureUtils.cs) from <path>/Modules/utils/CultureUtils.cs</path>
* Add the new Language enum and the language code to the `GetCultureInfoByLanguage` method.

    <img src="lang-culture-rider.png" alt="CultureUtils file"/>

## Testing the new language {collapsible="true" default-state="expanded"}
Now you can build and run FoliCon, and the new language should be available in the language dropdown.

Once you have verified that the new language works,
you can submit a pull request to add the new language to the main repository.

## Adding a new language from Crowdin {collapsible="true" default-state="collapsed"}

<warning>
FoliCon is currently in the process of being added to Crowdin.
It is recommended to use the IDE method for now. which can also be done using a simple text editor.
</warning>

[FoliCon Crowdin project](https://crowdin.com/project/folicon)

