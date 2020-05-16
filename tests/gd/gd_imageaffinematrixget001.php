<?php
//Can not use print_r because of different round precission between .Net(Peachpie) and PHP. 
function writeMatrix(array $matrix) : void
{
    foreach ($matrix as $key => $value) 
    {
        $val = round($value,14);
        echo "{$key} => {$val} \n";
    }
}


$matrix0 = imageaffinematrixget(IMG_AFFINE_TRANSLATE, array('x' => 2, 'y' => 3));
$matrix0d = imageaffinematrixget(IMG_AFFINE_TRANSLATE, array('x' => 2.5, 'y' => 3.5));
$matrix1 = imageaffinematrixget(IMG_AFFINE_SCALE, array('x' => 2, 'y' => 3));
$matrix2 = imageaffinematrixget(IMG_AFFINE_ROTATE, 45);
$matrix3 = imageaffinematrixget(IMG_AFFINE_SHEAR_HORIZONTAL, 10);
$matrix4 = imageaffinematrixget(IMG_AFFINE_SHEAR_VERTICAL, 10);

echo "Translate: ";
writeMatrix($matrix0);
echo "\n";
echo "Translate double: ";
writeMatrix($matrix0d);
echo "\n";
echo "Scale: ";
writeMatrix($matrix1);
echo "\n";
echo "Rotate: ";
writeMatrix($matrix2);
echo "\n";
echo "Horizontal: ";
writeMatrix($matrix3);
echo "\n";
echo "Vertical: ";
writeMatrix($matrix4);






