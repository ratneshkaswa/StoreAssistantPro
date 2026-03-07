$f = "Core\Styles\DesignSystem.xaml"
$lines = [System.Collections.ArrayList](Get-Content $f)
for ($i=0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'FluentTextOnAccent') {
        $lines.Insert($i+1, '    <SolidColorBrush x:Key="FluentTextOnAccentPressed" Color="#FFFFFF" Opacity="0.8"/>')
        break
    }
}
Set-Content $f -Value $lines -Encoding UTF8

$f2 = "Core\Styles\FluentTheme.xaml"
$content = [System.IO.File]::ReadAllText($f2)
$content = $content.Replace('Value="#CCFFFFFF"', 'Value="{StaticResource FluentTextOnAccentPressed}"')
[System.IO.File]::WriteAllText($f2, $content)
Write-Host "Fixed pressed text color"
