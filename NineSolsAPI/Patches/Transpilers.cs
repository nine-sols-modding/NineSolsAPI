using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace NineSolsAPI.Patches;

[HarmonyPatch]
public class Transpilers {
    // To include the modlist in the save metadata, transpiler inserts a CIL instruction 
    // that calls InsertModListBeforeSerialization() to put the modlist into the metadata string json before it gets serialized to binary.
    [HarmonyPatch(typeof(GameFlagManager), nameof(GameFlagManager.FlagsToBinary))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> FlagsToBinary_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var code = new List<CodeInstruction>(instructions);
        int insertionIndex = -1;
        for (int i = (code.Count - 1); i > 0; i--) { // Looks for the instruction that puts the save metadata string json on the stack.
            if (
                code[i - 1].opcode == OpCodes.Callvirt &&
                code[i].opcode == OpCodes.Call &&
                code[i + 1].opcode == OpCodes.Stfld
            ) {
                insertionIndex = i + 1;
                break;
            }
        }

        if (insertionIndex == -1) {
            NineSolsAPICore.Instance.titlescreenModifications.modList.transpilerFailed = true;
            Log.Error("GameFlagManager FlagsToBinary transpiler failed!");
            foreach (var instruction in instructions)
                Log.Error($"{instruction.opcode} {instruction.operand}");

            return instructions;
        }

        code.Insert(insertionIndex, CodeInstruction.Call(() => CustomFlagManager.InsertModListBeforeBinarySerialization(default)));
        return code;
    }

    public class ReadMetaInSave_TargetMethod {
        /*
        Because ReadMetaInSave() is an async method, the compiler turns it into a state machine. The CIL instructions that are needed to transpile requires HarmonyTargetMethod.
        HarmonyTargetMethod needs to be in its own class as it would otherwise affect the other transpilers.
        Even without changing anything, transpiler breaks for no reason unless I replace a specific instruction that I hope wasn't important for something else.
        */
        private static Type accessStruct = AccessTools.Inner(typeof(SaveManager), "<ReadMetaInSave>d__109");
        private static MethodInfo accessMethod = AccessTools.Method(accessStruct, "MoveNext");
        private static FieldInfo accessField = accessStruct.GetField("saveDirectory");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() {
            return accessMethod;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var code = new List<CodeInstruction>(instructions);

            int overwriteFixInsertionIndex = -1;
            for (int i = 1; i < code.Count; i++) {
                if (code[i].opcode == OpCodes.Newobj) {
                    overwriteFixInsertionIndex = i + 1;
                    break;
                }
            }
            int readMetaInsertionIndex = -1;
            for (int i = 1; i < code.Count; i++) {
                if (
                    code[i - 1].opcode == OpCodes.Call &&
                    code[i].opcode == OpCodes.Call
                ) {
                    readMetaInsertionIndex = i + 1;
                    break;
                }
            }
            
            if (overwriteFixInsertionIndex == -1 || readMetaInsertionIndex == -1) {
                NineSolsAPICore.Instance.titlescreenModifications.modList.transpilerFailed = true;
                Log.Error("<ReadMetaInSave>d__109 MoveNext transpiler failed!");
                foreach (var instruction in instructions)
                    Log.Error($"{instruction.opcode} {instruction.operand}");

                return null!; // As stated above, patching with the instructions unchanged somehow breaks it. Returning a null seems to have Harmony cancel the patch and leave the instructions alone.
            }

            code[overwriteFixInsertionIndex].opcode = OpCodes.Pop; // The "fix."

            var readMetaInstructions = new List<CodeInstruction>() {
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, accessField),
                new CodeInstruction(CodeInstruction.Call(() => CustomFlagManager.ReadModListBeforeJSONDeserialization(default, default)))
            };
            code.InsertRange(readMetaInsertionIndex, readMetaInstructions);

            return code;
        }
    }
}

