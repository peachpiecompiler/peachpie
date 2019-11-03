<?php
namespace strings\json_002_JsonSerializable;
class ArrayValue implements \JsonSerializable {

	var $array;

    public function __construct(array $array) {
        $this->array = $array;
    }

    public function jsonSerialize() {
        return $this->array;
    }
}

$array = [1, 2, 3];
echo json_encode(new ArrayValue($array)), "Done.";
