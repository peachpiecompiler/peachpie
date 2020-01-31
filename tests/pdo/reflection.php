<?php

namespace pdo\reflection;

function methods()
{
	// https://github.com/peachpiecompiler/peachpie/issues/523
	
	$cls = new \ReflectionClass(\PDO::class);

	$methods = \array_filter($cls->getMethods(), function ($item) {
		return \in_array($item->getName(), [
			"getAttribute",
			"setAttribute",
			"__construct",
			"getAvailableDrivers",
			"exec",
			"beginTransaction",
			"commit",
			"rollBack",
			"lastInsertId",
			"prepare",
			"query",
			"quote",
			"errorCode",
			"errorInfo"
		]);
	});

	usort($methods, function ($lhs, $rhs) {
		return \strcmp($lhs->getName(), $rhs->getName());
	});

	foreach ($methods as $method) {
		echo $method->getName(), ":", PHP_EOL;

		if ($method->isConstructor()) {
			echo " - isConstructor", PHP_EOL;
		}

		if ($method->isFinal()) {
			echo " - isFinal", PHP_EOL;
		}

		if ($method->isPrivate()) {
			echo " - isPrivate", PHP_EOL;
		}

		if ($method->isProtected()) {
			echo " - isProtected", PHP_EOL;
		}

		if ($method->isPublic()) {
			echo " - isPublic", PHP_EOL;
		}
	}
}

methods();

echo "Done.";
