<?php
namespace spl\RecursiveTreeIterator_001;

function print_tit(\RecursiveTreeIterator $tit) {
    foreach( $tit as $key => $value ){
        echo $value . PHP_EOL;
    }
}

function test() {
    $it = new \RecursiveArrayIterator(array(1, 2, array(3, 4, array(5, 6, 7), 8), 9, array(10, 11)));   
    $tit = new \RecursiveTreeIterator($it);
    print_tit($tit);

    $tit->setPrefixPart(\RecursiveTreeIterator::PREFIX_LEFT, ">");
    $tit->setPrefixPart(\RecursiveTreeIterator::PREFIX_MID_HAS_NEXT, "===");
    $tit->setPrefixPart(\RecursiveTreeIterator::PREFIX_MID_LAST, "---");
    $tit->setPrefixPart(\RecursiveTreeIterator::PREFIX_END_HAS_NEXT, ":");
    $tit->setPrefixPart(\RecursiveTreeIterator::PREFIX_END_LAST, ";");
    $tit->setPrefixPart(\RecursiveTreeIterator::PREFIX_RIGHT, "<");
    $tit->setPostfix(">>");
    print_tit($tit);
}

test();
