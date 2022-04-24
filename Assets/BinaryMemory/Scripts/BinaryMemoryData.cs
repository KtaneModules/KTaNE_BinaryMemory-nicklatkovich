using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public static class BinaryMemoryData {
	private static int _generatedSeed;
	private static int[][][] _priorities;

	public static int[][] GetPriorities(KMBombInfo bombInfo, int ruleSeedId) {
		if (_generatedSeed != ruleSeedId || _priorities == null) GeneratePriorities(ruleSeedId);
		if (bombInfo.GetPortPlateCount() > 2) return _priorities[0];
		if (bombInfo.GetSerialNumberLetters().Count() == 3) return _priorities[1];
		if (bombInfo.GetBatteryHolderCount() == 1) return _priorities[2];
		if (bombInfo.GetSolvableModuleIDs().Count >= 29) return _priorities[3];
		return _priorities[4];
	}

	private static void GeneratePriorities(int ruleSeedId) {
		MonoRandom rnd = new MonoRandom(ruleSeedId);
		_priorities = new int[5][][];
		for (int i = 0; i < 5; i++) {
			_priorities[i] = new int[10][];
			for (int j = 1; j <= 10; j++) _priorities[i][j % 10] = GeneratePriority(rnd);
		}
		_generatedSeed = ruleSeedId;
	}

	private static int[] GeneratePriority(MonoRandom rnd) {
		int[] res = Enumerable.Range(0, 4).ToArray();
		for (int i = 0; i < res.Length; i++) {
			int j = rnd.Next(res.Length);
			int temp = res[i];
			res[i] = res[j];
			res[j] = temp;
		}
		return res;
	}

	public static readonly string[] DefaultIgnoredModules = new[] {
		"Binary Memory",
		"+",
		"14",
		"42",
		"501",
		"8",
		"Access Codes",
		"Again",
		"Amnesia",
		"B-Machine",
		"A>N<D",
		"Bamboozling Time Keeper",
		"Black Arrows",
		"Brainf---",
		"Busy Beaver",
		"Button Messer",
		"Castor",
		"Channel Surfing",
		"Concentration",
		"Cookie Jars",
		"Cube Synchronization",
		"Custom Keys",
		"Divided Squares",
		"Don't Touch Anything",
		"Doomsday Button",
		"Duck Konundrum",
		"Encrypted Hangman",
		"Encryption Bingo",
		"Floor Lights",
		"Forget Any Color",
		"Forget Everything",
		"Forget Infinity",
		"Forget Enigma",
		"Forget Maze Not",
		"Forget It Not",
		"Forget Me Later",
		"Forget Me Maybe",
		"Forget Me Not",
		"Forget Morse Not",
		"Forget Perspective",
		"Forget This",
		"Forget Them All",
		"Forget The Colors",
		"Forget Our Voices",
		"Forget Us Not",
		"Four-Card Monte",
		"Gemory",
		"Hogwarts",
		"HyperForget",
		"Iconic",
		"ID Exchange",
		"Keypad Directionality",
		"Kugelblitz",
		"Multitask",
		"Mystery Module",
		"OmegaDestroyer",
		"OmegaForget",
		"Organization",
		"Out of Time",
		"Password Destroyer",
		"Peek-A-Boo",
		"Pollux",
		"Purgatory",
		"Red Light Green Light",
		"Remember Simple",
		"Remembern't Simple",
		"RPS Judging",
		"Scrabble Scramble",
		"Security Council",
		"Shoddy Chess",
		"Simon Forgets",
		"Simon Sonundrum",
		"Simon's Stages",
		"Simon",
		"Soulsong",
		"Soulscream",
		"Souvenir",
		"Speedrun",
		"SUSadmin",
		"Tallordered Keys",
		"Tetrahedron",
		"The Board Walk",
		"The Grand Prix",
		"The Heart",
		"The Klaxon",
		"The Swan",
		"The Time Keeper",
		"The Troll",
		"The Twin",
		"The Very Annoying Button",
		"Timing is Everything",
		"Turn The Keys",
		"Twister",
		"Ultimate Custom Night",
		"Ultra Custom Night",
		"Turn The Key",
		"Whiteout",
		"X",
		"Y",
		"Zener Cards",
		"[BIG SHOT]",
		"Übermodule",
	};
}
