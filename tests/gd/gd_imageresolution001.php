<?php
$im = imagecreatetruecolor(300,400);

echo "Resolution of the image:\n";
print_r(imageresolution($im));

echo "Set x to 0 and y to 0:\n";
print_r(imageresolution($im, 0,0));

echo "New resolution:\n";
print_r(imageresolution($im));

echo "Set x to 1:\n";
print_r(imageresolution($im, 1));

echo "New resolution:\n";
print_r(imageresolution($im));

echo "Set x to 96:\n";
print_r(imageresolution($im, 96));

echo "Set x to 300 and y to 72:\n";
print_r(imageresolution($im, 300,72));

echo "New resolution:\n";
print_r(imageresolution($im));

echo "Set x to 96:\n";
print_r(imageresolution($im, 96));

echo "New resolution:\n";
print_r(imageresolution($im));

//wierd functionality
//echo "Set x to -300 and y to -72:\n";
//print_r(imageresolution($im, -300,-72));

echo "New resolution:\n";
print_r(imageresolution($im));
?>