using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using KeepCoding;
using KModkit;

public class BinaryMemoryModule : ModuleScript {
	private const int MAX_STAGES_COUNT = 45;
	private const int MAX_SOLVING_SOUNDS = 10;
	private const float KEYS_INTERVAL = 0.0289378f;
	private const float LEDS_INTERVAL = 0.018f;
	private const float SOLVING_ANIMATION_DURATION = 2f;
	private readonly Color COLOR_0 = new Color(0.8f, 0.8f, 0.8f, 0.8f);
	private readonly Color COLOR_1 = new Color(1f, 0f, 0f, 0.8f);
	private readonly Color COLOR_2 = new Color(0f, 1f, 0f, 0.8f);
	private readonly Color COLOR_SOLVE = Color.green;

	public readonly string TwitchHelpMessage = "\"!{0} 1234\" - Press buttons";

	public Transform KeysContainer;
	public Transform LEDsContainer;
	public TextMesh Display;
	public KMSelectable Selectable;
	public KMAudio Audio;
	public KMBossModule BossModule;
	public KMBombInfo BombInfo;
	public KeyComponent KeyPrefab;
	public ModeLED ModeLEDPrefab;

	private int _mode = 0;
	public int Mode { get { return _mode; } set { if (_mode == value) return; _mode = value; UpdateLEDs(); } }

	private bool _lastStageIsFinale;
	private int _passedStagesCount = -1;
	private int _recoveryStreak;
	private int _recoveryLastPress;
	private float _solvingAnimationStartingTime = -1;
	private bool[] _recoveryPressed;
	private bool[] _recoveryLastValue;
	private int[] _expectedAnswer;
	private KeyComponent[] _buttons;
	private ModeLED[] _leds;
	private bool[] _stages;
	private HashSet<string> _ignoredModules;

	private void Start() {
		List<KeyComponent> buttons = new List<KeyComponent>();
		for (int i = 0; i < 4; i++) {
			KeyComponent key = Instantiate(KeyPrefab);
			key.transform.parent = KeysContainer;
			key.transform.localPosition = Vector3.right * KEYS_INTERVAL * i;
			key.transform.localScale = Vector3.one;
			key.transform.localRotation = Quaternion.identity;
			key.Label.text = (i + 1).ToString();
			key.Selectable.Parent = Selectable;
			buttons.Add(key);
		}
		Selectable.Children = buttons.Select(k => k.Selectable).ToArray();
		Selectable.UpdateChildrenProperly();
		_buttons = buttons.ToArray();
		List<ModeLED> leds = new List<ModeLED>();
		for (int i = 0; i < 5; i++) {
			ModeLED led = Instantiate(ModeLEDPrefab);
			led.transform.parent = LEDsContainer;
			led.transform.localPosition = Vector3.forward * (i - 2f) * LEDS_INTERVAL;
			led.transform.localScale = Vector3.one;
			led.transform.localRotation = Quaternion.identity;
			leds.Add(led);
		}
		_leds = leds.ToArray();
	}

	public override void OnActivate() {
		base.OnActivate();
		_ignoredModules = new HashSet<string>(BossModule.GetIgnoredModules("Binary Memory", BinaryMemoryData.DefaultIgnoredModules));
		List<string> allModules = BombInfo.GetSolvableModuleNames();
		int unignoredModulesCount = allModules.Where(m => !_ignoredModules.Contains(m)).Count();
		int maxStagesCount = Mathf.Min(unignoredModulesCount + 1, MAX_STAGES_COUNT);
		Log("Max stages count: {0}", maxStagesCount);
		List<bool> stages = new List<bool>();
		int streak = 0;
		while (stages.Count < maxStagesCount && streak < 4) {
			bool newStage = Random.Range(0, 2) == 0;
			if (newStage == stages.LastOrDefault()) streak += 1;
			else streak = 1;
			stages.Add(newStage);
		}
		Log("Generated stages count: {0}", stages.Count);
		Log("Stages: {0}", stages.Select(s => s ? '1' : '0').Join(""));
		_lastStageIsFinale = stages.Count == maxStagesCount;
		if (!_lastStageIsFinale) Log("You must press any key at stage #{0} of reading mode", stages.Count);
		_stages = stages.ToArray();
		CalcSolution();
		Mode = stages.Count == 1 ? 2 : 1;
		Display.text = "1";
		Display.color = _stages[0] ? COLOR_2 : COLOR_1;
		_passedStagesCount = 0;
		for (int i = 0; i < _buttons.Length; i++) {
			int iI = i;
			_buttons[i].Selectable.OnInteract += () => { PressButton(iI); return false; };
		}
	}

	private void Update() {
		if (Mode != 1) return;
		int stageIndex = BombInfo.GetSolvedModuleNames().Where(s => !_ignoredModules.Contains(s)).Count();
		if (stageIndex >= _stages.Length) {
			stageIndex = _stages.Length - 1;
			if (!_lastStageIsFinale) {
				Log("No key was pressed at requred stage. Strike!");
				Strike();
				StartSubmitMode();
				return;
			}
		}
		if (stageIndex + 1 == _stages.Length && _lastStageIsFinale) Mode = 2;
		if (stageIndex == _passedStagesCount) return;
		_passedStagesCount = stageIndex;
		Display.text = (_passedStagesCount + 1).ToString();
		Display.color = _stages[_passedStagesCount] ? COLOR_2 : COLOR_1;
	}

	public void PressButton(int index) {
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (Mode == 0) return;
		if (Mode == 1) {
			if (_lastStageIsFinale || _passedStagesCount + 1 < _stages.Length) {
				Log("Unexpected button press. Strike!");
				Strike();
			} else StartSubmitMode();
		} else if (Mode == 2) StartSubmitMode();
		else if (Mode == 3) {
			if (index == _expectedAnswer[_passedStagesCount]) {
				_recoveryPressed[index] = true;
				_recoveryLastValue[index] = _stages[_passedStagesCount];
				_passedStagesCount += 1;
				if (_passedStagesCount == _stages.Length) {
					Log("Module solved");
					Mode = 5;
					StartCoroutine(StartSolvingAnimation());
				} else Display.text = (_passedStagesCount + 1).ToString();
			} else {
				Log("Pressed button {0} at stage #{1}. Expected button to press: {2}. Strike!", index + 1, _passedStagesCount + 1, _expectedAnswer[_passedStagesCount] + 1);
				Strike();
				Mode = 4;
				_recoveryStreak = 1;
				_recoveryLastPress = -1;
				Display.color = _stages[_passedStagesCount] ? COLOR_2 : COLOR_1;
			}
		} else if (Mode == 4) {
			if (_recoveryLastPress == index) _recoveryStreak += 1;
			else _recoveryStreak = 1;
			_recoveryLastPress = index;
			if (_recoveryStreak == 1) Display.color = _recoveryPressed[index] ? (_recoveryLastValue[index] ? COLOR_2 : COLOR_1) : COLOR_0;
			else if (_recoveryStreak == 2) Display.color = _stages[_passedStagesCount] ? COLOR_2 : COLOR_1;
			else {
				Mode = 3;
				Display.color = COLOR_0;
			}
		}
	}

	public IEnumerator ProcessTwitchCommand(string command) {
		command = command.ToLower().Trim();
		if (command.StartsWith("press ")) command = command.Skip(6).Join("").Trim();
		command = command.Split(' ').Where(s => s.Length > 0).Join("");
		if (!Regex.IsMatch(command, @"^[1-4]+$")) yield break;
		yield return null;
		yield return command.Select(c => _buttons[c - '1'].Selectable).ToArray();
		if (_solvingAnimationStartingTime >= 0) yield return string.Format("awardpoints {0}", _stages.Length / 2);
	}

	private IEnumerator StartSolvingAnimation() {
		_solvingAnimationStartingTime = Time.time;
		int prevStageIndex = -1;
		int playedSounds = -1;
		bool independedSounds = _stages.Length > MAX_SOLVING_SOUNDS;
		while (true) {
			float timeDiff = Time.time - _solvingAnimationStartingTime;
			int stageIndex = Mathf.FloorToInt(_stages.Length * timeDiff / SOLVING_ANIMATION_DURATION);
			if (stageIndex >= _stages.Length) break;
			if (prevStageIndex != stageIndex) {
				if (!independedSounds) Audio.PlaySoundAtTransform("SolvingSound", transform);
				prevStageIndex = stageIndex;
				Display.text = (stageIndex + 1).ToString();
				Display.color = _stages[stageIndex] ? COLOR_2 : COLOR_1;
			}
			if (independedSounds) {
				int soundsToPlay = Mathf.FloorToInt(MAX_SOLVING_SOUNDS * timeDiff / SOLVING_ANIMATION_DURATION);
				if (playedSounds < soundsToPlay) {
					Audio.PlaySoundAtTransform("SolvingSound", transform);
					playedSounds = soundsToPlay;
				}
			}
			yield return null;
		}
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		Solve();
		Display.text = "GG";
		Display.color = COLOR_SOLVE;
	}

	private void TwitchHandleForcedSolve() {
		Log("Module was force-solved");
		Mode = 5;
		StartCoroutine(StartSolvingAnimation());
	}

	private void StartSubmitMode() {
		Log("Entering submit mode");
		Mode = 3;
		Display.text = "1";
		Display.color = COLOR_0;
		_passedStagesCount = 0;
		_recoveryPressed = Enumerable.Range(0, 4).Select(_ => false).ToArray();;
		_recoveryLastValue = Enumerable.Range(0, 4).Select(_ => false).ToArray();
	}

	private void UpdateLEDs() {
		for (int i = 0; i < _leds.Length; i++) _leds[i].On = i < Mode;
	}

	private void CalcSolution() {
		bool[] _pressed = Enumerable.Range(0, 4).Select(_ => false).ToArray();
		bool[] _lastPressValue = Enumerable.Range(0, 4).Select(_ => false).ToArray();
		_expectedAnswer = new int[_stages.Length];
		int[][] priorities = BinaryMemoryData.GetPriorities(BombInfo, RuleSeedId);
		for (int i = 0; i < _stages.Length; i++) {
			int[] priority = priorities[(i + 1) % 10];
			for (int j = 0; j < priority.Length; j++) {
				int btnIndex = priority[j];
				if (_pressed[btnIndex] && _lastPressValue[btnIndex] == _stages[i]) continue;
				_pressed[btnIndex] = true;
				_lastPressValue[btnIndex] = _stages[i];
				_expectedAnswer[i] = btnIndex;
				break;
			}
		}
		Log("Expected answer: {0}", _expectedAnswer.Select(i => i + 1).Join(""));
	}
}
