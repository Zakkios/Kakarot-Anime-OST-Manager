# Convertit l'image de l'icône (PNG) en fichier .ico utilisable par Windows.
#
# Un .ico regroupe la même image en plusieurs tailles ; Windows choisit la
# plus adaptée selon l'endroit (barre de titre, Explorateur, barre des tâches).
#
# Utilisation, depuis la racine du dépôt, chaque fois que app.png change :
#   pwsh tools/make-icon.ps1

param(
    [string]$Source = (Join-Path $PSScriptRoot '..\src\KakarotOstManager\Assets\app.png'),
    [string]$OutFile = (Join-Path $PSScriptRoot '..\src\KakarotOstManager\Assets\app.ico')
)

Add-Type -AssemblyName System.Drawing

function New-ResizedPng {
    # Renvoie l'image réduite à $size pixels de côté, encodée en PNG.
    param([System.Drawing.Image]$image, [int]$size)

    $bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    # Sans ce réglage, la réduction laisse un liseré sombre sur les bords.
    $attributes = New-Object System.Drawing.Imaging.ImageAttributes
    $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
    $target = New-Object System.Drawing.Rectangle 0, 0, $size, $size
    $g.DrawImage($image, $target, 0, 0, $image.Width, $image.Height, [System.Drawing.GraphicsUnit]::Pixel, $attributes)

    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $attributes.Dispose(); $g.Dispose(); $bitmap.Dispose()
    return , $stream.ToArray()
}

$Source = [System.IO.Path]::GetFullPath($Source)
$OutFile = [System.IO.Path]::GetFullPath($OutFile)
$image = [System.Drawing.Image]::FromFile($Source)
if ($image.Width -ne $image.Height) {
    Write-Warning "L'image n'est pas carrée ($($image.Width) x $($image.Height)) : l'icône sera déformée."
}

$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$images = New-Object 'System.Collections.Generic.List[byte[]]'
foreach ($size in $sizes) {
    $images.Add((New-ResizedPng $image $size))
}
$image.Dispose()

# Écriture du fichier : un en-tête, une entrée de 16 octets par image, puis les images.
$writer = New-Object System.IO.BinaryWriter ([System.IO.File]::Create($OutFile))
$writer.Write([uint16]0)                # réservé
$writer.Write([uint16]1)                # 1 = icône
$writer.Write([uint16]$sizes.Count)     # nombre d'images

[uint32]$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dimension = [byte]($sizes[$i] -band 0xFF)   # 256 s'écrit 0
    $writer.Write($dimension)                    # largeur
    $writer.Write($dimension)                    # hauteur
    $writer.Write([byte]0)                       # nombre de couleurs (0 = pas de palette)
    $writer.Write([byte]0)                       # réservé
    $writer.Write([uint16]1)                     # plans
    $writer.Write([uint16]32)                    # bits par pixel
    $writer.Write([uint32]$images[$i].Length)    # taille de l'image
    $writer.Write($offset)                       # position de l'image dans le fichier
    $offset += $images[$i].Length
}
foreach ($data in $images) { $writer.Write($data) }
$writer.Dispose()

"Icône écrite : $OutFile ($((Get-Item $OutFile).Length) octets, $($sizes.Count) tailles) à partir de $Source"
