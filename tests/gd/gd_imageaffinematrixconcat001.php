<?php
//Can not use print_r because of different round precission between .Net(Peachpie) and PHP. 
function writeMatrix(array $matrix) : void
{
    foreach ($matrix as $key => $value)
    {
        $val = round($value,5);
        echo "{$key} => {$val} \n";
    }
}

$m1 = imageaffinematrixget(IMG_AFFINE_TRANSLATE, array('x' => 2, 'y' => 3));
$m2 = imageaffinematrixget(IMG_AFFINE_SCALE, array('x' => 4, 'y' => 5));
$matrix1 = imageaffinematrixconcat($m1, $m2);

$m3 = imageaffinematrixget(IMG_AFFINE_ROTATE, 45);
$m4 = imageaffinematrixget(IMG_AFFINE_SCALE, array('x' => 4.4, 'y' => 5.5));
$matrix2 = imageaffinematrixconcat($m3, $m4);

echo "matrix1: ";
writeMatrix($matrix1);
echo "\n";
echo "matrix2: ";
writeMatrix($matrix2);
