<?php
/*
 * Twitch Authentication Asset by Firesplash Entertainment
 * This file is licensed under Unity Asset Store License
 * You may only use this code, if the legal entitiy this is being used by owns at least one copy of "Twitch Authentication" asset by Firesplash Entertainment.
 *
 * This script is a very simple helper to keep your Client Secret safe. It can be configured to assist logging in multible games/apps with easy configuration
 * You can configure your client secrets by copying the example code block and replacing the configured data.
 *
 * Using HTTPS is highly recommended to protect the privacy of your players.
 * Hint: You could also set up some rate limiting in your webserver to enhance security.
 */
 
 
 
$CONFIG = [ //Don't touch this line



//Add one block per "Unique Identifier" here. The said identifier is the value your inspector shows (or you configured) in the respective field.
//Make sure that every block ends with a comma (,) - Don't forget to enter the URL to this script back into the inspector.



//////// START CONFIGURATION ////////


	"EnterUniqueIdHere" => [						//Replace the text between the double-quotes with your Unique Identifier from the unity inspector
		'client' 		=>	"EnterClientIdHere",		//Replace the text between the double-quotes with your twitch Client ID
		'secret' 		=>	"EnterClientSecretHere",	//Replace the text between the double-quotes with your twitch Client Secret
		'paroles'		=>	[]							//Advanced topic: If you add paroles here, you must configure one of them into the inspector. Any request without a valid parole will be denied with error code 400 just like any other configuration error. Parole-enabled blocks MUST NOT use HTTP, only HTTPS is supported. Keeping an empty array ([]) disables this feature.
	],

	//add further blocks here


////////  END CONFIGURATION  ////////




////////////////////////////////////////
//Do not edit anything below this line//
////////////////////////////////////////
];


if (!function_exists('curl_init')) {
	die('This script requires the curl extension and at least PHP 5 to work properly. Please install php-curl');
}

if (isset($_GET['ident']) && array_key_exists($_GET['ident'], $CONFIG)) {
	if (isset($CONFIG[$_GET['ident']]['paroles']) && count($CONFIG[$_GET['ident']]['paroles']) > 0) {
		if (!isset($_SERVER['HTTPS']) || empty($_SERVER['HTTPS']) || strtolower($_SERVER['HTTPS']) == 'off') {
			http_response_code(400);
			die('I don\'t like listeners. Talk to me in private! (Use SSL/HTTPS)');
		}
		
		if (!in_array($_SERVER['HTTP_X_PAROLE'], $CONFIG[$_GET['ident']]['paroles'])) {
			http_response_code(400);
			die('Game not authorized - Check for updates, if you are a player.');
		}
	}
	
	if ($_GET['port'] < 1025 || $_GET['port'] > 65535 || strlen($_GET['action']) < 2 || strlen($_GET['code']) < 3) {
		http_response_code(400);
			die('Invalid payload');
	}
	
	if ($_GET['action'] == 'get') 
	{
		//Trade authorization code for access token
		$ch = curl_init('https://id.twitch.tv/oauth2/token');
		curl_setopt($ch, CURLOPT_POST, 1);
		curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
		curl_setopt($ch, CURLOPT_POSTFIELDS, http_build_query([
			'client_id' => $CONFIG[$_GET['ident']]['client'],
			'client_secret' => $CONFIG[$_GET['ident']]['secret'],
			'code' => $_GET['code'],
			'grant_type' => 'authorization_code',
			'redirect_uri' => 'http://localhost:'.$_GET['port']
		]));
		
		$response = curl_exec($ch);
		$status = curl_getinfo($ch, CURLINFO_HTTP_CODE);
		
		if (curl_errno($ch) != 0 && $status < 200) {
			http_response_code(500);
			die('Service Error: ' . curl_error($ch));
		}
		
		curl_close($ch);
		
		http_response_code($status);
		print $response;
	}
	else if ($_GET['action'] == 'refresh') 
	{
		//Refresh an access token using a refresh token
		$ch = curl_init('https://id.twitch.tv/oauth2/token');
		curl_setopt($ch, CURLOPT_POST, 1);
		curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
		curl_setopt($ch, CURLOPT_POSTFIELDS, http_build_query([
			'client_id' => $CONFIG[$_GET['ident']]['client'],
			'client_secret' => $CONFIG[$_GET['ident']]['secret'],
			'grant_type' => 'refresh_token',
			'refresh_token' => $_GET['code']
		]));
		
		$response = curl_exec($ch);
		$status = curl_getinfo($ch, CURLINFO_HTTP_CODE);
		
		if (curl_errno($ch) != 0 && $status < 200) {
			http_response_code(500);
			die('Service Error: ' . curl_error($ch));
		}
		
		curl_close($ch);
		
		http_response_code($status);
		print $response;
	}
} else {
	http_response_code(400);
	die('Game\'s request did not match configuration. Please check Unique ID configuration.');
}
?>