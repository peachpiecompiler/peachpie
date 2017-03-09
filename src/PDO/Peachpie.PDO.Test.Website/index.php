<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Peachpie PDO</title>

    <!-- Latest compiled and minified CSS -->
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css" integrity="sha384-BVYiiSIFeK1dGmJRAkycuHAHRg32OmUcww7on3RYdg4Va+PmSTsz/K68vbdEjh4u" crossorigin="anonymous" />

    <!-- Optional theme -->
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap-theme.min.css" integrity="sha384-rHyoN1iRsVXV4nD0JutlnGaslCJuC7uwjduW9SVrLvRYooPp2bWYgmgJQIXwl/Sp" crossorigin="anonymous" />

    <!-- Latest compiled and minified JavaScript -->
    <script src="https://code.jquery.com/jquery-2.2.4.min.js" crossorigin="anonymous"></script>
    <script src="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js" integrity="sha384-Tc5IQib027qvyjSMfHjOMaLkfuWVxZxUPnCJA7l2mCWNIpG9mGCD8wGNIcPD7Txa" crossorigin="anonymous"></script>
</head>
<body>
    <div class="container">
        <div class="row">
            <div class="col-lg-4">
                <div class="panel panel-default">
                    <div class="panel-heading">
                        <h1 class="panel-title">PDO Drivers</h1>
                    </div>
                    <ul>
                    <?php
                        foreach(PDO::getAvailableDrivers() as $driverName)
                        {
                            echo "<li>".htmlentities($driverName)."</li>";
                        }
                    ?>
                    </ul>
                </div>
            </div>
        </div>
    </div>
<?php

/**
Some tests, don't care :)
*/
echo "<hr />";
$pdo = new PDO('sqlite::memory:');
$result = $pdo->exec("CREATE TABLE data (id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, value VARCHAR(100) NULL);");
var_dump($result);
echo "<br />";
$result = $pdo->exec("INSERT INTO data (value) VALUES ('test')");
var_dump($result);
$id = $pdo->lastInsertId();
var_dump($id);
echo "<br />";

echo "<hr />";
echo "<p>the end.</p>";
?>
</body>
</html>