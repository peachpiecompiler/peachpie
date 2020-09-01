<?php
namespace spl\AppendIterator_002;

function test() {
  $arrIt = new \ArrayIterator(array('foo', 'bar', 'baz'));
  $filterIt = new \CallbackFilterIterator($arrIt, function ($item) { return true; });

  $appendIt = new \AppendIterator;
  $appendIt->append($filterIt);

  foreach ($appendIt as $key => $item) {
    echo $appendIt->getIteratorIndex() . ' => ' . $key . ' => ' . $item . "\n";
  }
}

test();
