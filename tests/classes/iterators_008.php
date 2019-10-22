<?php
namespace classes\iterators_008;

class MyArrayIterator extends \RecursiveArrayIterator
{
    public function __construct($it, $flags = 0, $magic_no = -1) {
      parent::__construct($it, $flags);
      // Called from getChildren() with just one parameter
      echo "MyArrayIterator constructed with magic_no = {$magic_no}\n";

      // TODO: Discover where the flags like 2031616 or 69140480 in Zend PHP come from (when constructed in getChildren()) and then enable
      //echo "MyArrayIterator constructed with flags = $flags\n";
    }
}

class MyFilterIterator extends \RecursiveFilterIterator
{
    public function accept() {
      $cur = $this->current();
      return is_array($cur) || $cur % 2 == 0;
    }
    
    public function __construct($it, $magic_no = -1) {
      parent::__construct($it);
      // Called from getChildren() with just one parameter
      echo "MyFilterIterator constructed with magic_no = {$magic_no}\n";
    }
}

$arr = array(
    array(0, 1, 2),
    2,
    3,
    array(4, 5, 6));

$arrIt = new MyArrayIterator($arr, 0, 42);
$myIt = new MyFilterIterator($arrIt, 42);

$myIt->rewind();
print_r($myIt->current());
echo ($myIt->hasChildren() ? "true" : "false") ."\n";

$chld = $myIt->getChildren();
echo get_class($chld) ."\n";
echo get_class($myIt->getInnerIterator()) ."\n";
echo get_class($chld->getInnerIterator()) ."\n";
//print_r($chld); echo "\n";    // TODO: Enable when it doesn't cause stack overflow by enumerating Context

// Children iterator must be independent on the top level one (it is created in getChildren() dynamically)
echo ($myIt === $chld ? "identical" : "not identical" ) ."\n";
echo ($myIt->getInnerIterator() === $chld->getInnerIterator() ? "identical" : "not identical" ) ."\n";
$chld->rewind();
$chld->next();
print_r($myIt->current()); echo "\n";
print_r($chld->current()); echo "\n";

$myIt->next();
echo ($myIt->hasChildren() ? "true" : "false") ."\n";
