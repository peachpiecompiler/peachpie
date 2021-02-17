<?php
// Creates img
$width = 9;
$height = 9;
$img = imagecreatetruecolor($width, $height);

//functions
function draw_init_symbol($img)
{
    $width  = imagesx($img);
    $height = imagesy($img);
    $color = imagecolorallocate($img, 255, 255, 255);

    // Draw verticle axis
    for ($j = 0; $j <  $height; $j++)
        imagesetpixel($img, $width/2, $j, $color);
    
    // Draw decreasing axis
    for ($j = 0;$j <  $height; $j++)
        imagesetpixel($img, $j, $j, $color);

    return $img;
}

function print_img_pixels($img)
{
    $width  = imagesx($img);
    $height = imagesy($img);
    for ($i = 0;$i <  $width;$i++)
    {
        echo "Row "  . $i . ": "; 
        for ($j = 0;$j <  $height;$j++)
        {
            echo imagecolorat($img, $i, $j) . " ";
        }
        echo "\n";
    }
}

function transform_img($img)
{
    $size = min(imagesx($img), imagesy($img));

    return imagecrop($img, ['x' => 1, 'y' => 1, 'width' => $size -2, 'height' => $size -2]);
}
// Init
$init_img = draw_init_symbol($img);

//Prints
echo "Before transformation img(". imagesx($init_img) . "x". imagesy($init_img) . "):\n";
print_img_pixels($init_img);

// Transforms img
$transformed = transform_img($init_img);

//Prints
if (!$transformed)
    echo "Transformation failed";
else
{
    echo "After transformation img(". imagesx($transformed) . "x". imagesy($transformed) . "):\n";
    print_img_pixels($transformed);
}
print_r(imagecolorsforindex($img,imagecolorat($img, 0, 0)));
?>