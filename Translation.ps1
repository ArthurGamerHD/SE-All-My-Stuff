Push-Location "C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Content\Data"
$FilesWithItems = Get-ChildItem -File | Where-Object -Property BaseName -NotLike 'Blueprint*' | Select-String -SimpleMatch "DisplayName_Item" -List | Select-Object -ExpandProperty Filename
$Localization = @{}
Select-Xml -Path .\Localization\MyTexts.resx -XPath "root/data" | Select-Object -ExpandProperty Node | ForEach-Object {$Localization[$_.name] = $_.value}
#Select-Xml -Path .\Localization\MyTexts.da.resx -XPath "root/data" | Select-Object -ExpandProperty Node | Where-Object {$_.value -ne $null} | ForEach-Object {$Localization[$_.name] = $_.value}

function Get-ItemsFromSBC {
    param (
        $SBCFile
    )
    $BaseName = Get-ChildItem $SBCFile | Select-Object -ExpandProperty Basename
    Select-Xml -Path $SBCFile -XPath ('Definitions',$BaseName,($BaseName.SubString(0,$BaseName.Length - 1)) -Join '/') |
        Select-Object -ExpandProperty Node |
        Select-Object @{
            Name='TypeId'
            Expression={
                $_.Id.TypeID
            }
        }, `
        @{
            Name='SubtypeId'
            Expression={
                $_.Id.SubtypeID
            }
        }, `
        @{
            Name='TypeDef'
            Expression={
                $_.Id.TypeID,$_.Id.SubTypeID -Join '/'
            }
        }, `
        DisplayName,
        @{
            Name='Localized'
            Expression={
                $Localization[$_.DisplayName]
            }
        }
}

Write-Host '[Translation]'
foreach($File in $FilesWithItems) {
    Get-ItemsFromSBC $File |
        Where-Object {$_.TypeId -ne 'TreeObject'} |
        Select-Object @{Name='Translation';expression={"$($_.TypeDef)=$($_.Localized)"}} |
        Select-Object -ExpandProperty Translation |
        Write-Host
}

Pop-Location
