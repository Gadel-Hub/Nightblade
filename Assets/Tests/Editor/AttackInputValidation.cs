using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nightblade.Tests
{
    public static class AttackInputValidation
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void Run()
        {
            PlayerCombat combat = null;
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatLab.unity");
                Require(scene.IsValid() && scene.isLoaded, "CombatLab did not open");
                combat = UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
                Require(combat != null, "CombatLab player is missing PlayerCombat");

                Invoke(combat, "Awake");
                Invoke(combat, "OnEnable");
                InputActionAsset serializedActions = GetField<InputActionAsset>(combat, "inputActions");
                InputActionAsset runtimeActions = GetField<InputActionAsset>(combat, "runtimeActions");
                InputAction attack = GetField<InputAction>(combat, "attackAction");
                Require(AssetDatabase.GetAssetPath(serializedActions) == "Assets/Input/Movement.inputactions",
                    "PlayerCombat references the wrong InputActionAsset");
                Require(runtimeActions != null && runtimeActions != serializedActions,
                    "PlayerCombat did not create its runtime InputActionAsset instance");
                Require(attack != null && attack.enabled, "Player/Attack was not resolved and enabled");
                Require(attack.actionMap.asset == runtimeActions,
                    "PlayerCombat reads Attack from a different action instance");
                Require(attack.bindings.Count == 1 && attack.bindings[0].effectivePath == "<Keyboard>/j",
                    "Player/Attack is not bound to J");

                Invoke(combat, "OnAttackPerformed", default(InputAction.CallbackContext));
                Require(GetField<bool>(combat, "attackRequested"),
                    "Attack performed callback did not queue a gameplay request");
                Invoke(combat, "FixedUpdate");
                Require(combat.Phase == PlayerCombat.AttackPhase.Startup,
                    "Queued request did not begin attack startup");
                Require(!GetField<bool>(combat, "attackRequested"), "Attack request was not consumed");

                combat.InterruptAttack();
                Invoke(combat, "FixedUpdate");
                Require(combat.Phase == PlayerCombat.AttackPhase.Idle,
                    "One performed callback generated more than one attack");

                Debug.Log("PASS: enabled Player/Attack J callback queues and consumes exactly one request.");
                Cleanup(combat);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (combat != null) Cleanup(combat);
                EditorApplication.Exit(1);
            }
        }

        private static void Cleanup(PlayerCombat combat)
        {
            Invoke(combat, "OnDisable");
            InputActionAsset runtimeActions = GetField<InputActionAsset>(combat, "runtimeActions");
            SetField(combat, "runtimeActions", null);
            if (runtimeActions != null) UnityEngine.Object.DestroyImmediate(runtimeActions);
        }

        private static void Invoke(object target, string methodName, params object[] arguments) =>
            target.GetType().GetMethod(methodName, PrivateInstance).Invoke(target, arguments);

        private static T GetField<T>(object target, string fieldName) =>
            (T)target.GetType().GetField(fieldName, PrivateInstance).GetValue(target);

        private static void SetField(object target, string fieldName, object value) =>
            target.GetType().GetField(fieldName, PrivateInstance).SetValue(target, value);

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
