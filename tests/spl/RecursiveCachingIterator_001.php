<?php
namespace spl\RecursiveCachingIterator_001;

function test() {
    $a = array(
        "First category" => "Lorem Ipsum",
        "Second category" => array(
            "Subcategory 1" => "Dolor Sit",
            "Subcategory 2" => array(
                "Subsubcategory 1" => "Amet",
                "Subsubcategory 2" => "Alibiscit",
                "Subsubcategory 3" => "Elit"
            ),
            "Subcategory 3" => "Dolor Sit",
        ),
        "Third category" => "Consecteurer"
    );
    $cached = new \RecursiveCachingIterator(new \RecursiveArrayIterator($a), \RecursiveCachingIterator::TOSTRING_USE_CURRENT);
    $it = new \RecursiveIteratorIterator($cached);

    foreach ($it as $key => $val) {
        for ($i = 0; $i < $it->getDepth(); $i++)
            echo "  ";
        echo "{$key}: {$val}";

        // Use \RecursiveCachingIterator to check if there is any item following on the current level
        if (!$it->getInnerIterator()->hasNext()) {
            echo " (last)";
        }
        echo "\n";
    }
}

test();
