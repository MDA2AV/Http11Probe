<?php
header('Content-Type: text/plain');
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    echo file_get_contents('php://input');
} else {
    echo 'OK';
}
