using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberTheater.Video;

namespace BeatSaberTheater.Settings;

public class VideoFilterRegister
{
    public static Func<bool> CreateVideoFilterIfBetterSongListPresent()
    {
        try
        {
            Assembly? betterSongListAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "BetterSongList");
            if (betterSongListAssembly == null)
            {
                throw new InvalidOperationException("BetterSongList assembly is not loaded in the AppDomain");
            }

            Type iFilterType = betterSongListAssembly.GetType("BetterSongList.FilterModels.IFilter");
            Type iTransformerPluginType = betterSongListAssembly.GetType("BetterSongList.Interfaces.ITransformerPlugin");

            AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

            TypeBuilder typeBuilder = moduleBuilder.DefineType("BeatSaberTheater.Settings.HasVideoFilter",
                TypeAttributes.Public | TypeAttributes.Class);

            typeBuilder.AddInterfaceImplementation(iFilterType);
            typeBuilder.AddInterfaceImplementation(iTransformerPluginType);

            PropertyBuilder isReadyProperty = typeBuilder.DefineProperty("isReady", PropertyAttributes.None, typeof(bool), null);
            MethodBuilder isReadyGetMethod = typeBuilder.DefineMethod("get_isReady",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName,
                typeof(bool), Type.EmptyTypes);
            ILGenerator isReadyIL = isReadyGetMethod.GetILGenerator();
            isReadyIL.Emit(OpCodes.Ldc_I4_1);
            isReadyIL.Emit(OpCodes.Ret);
            isReadyProperty.SetGetMethod(isReadyGetMethod);

            PropertyBuilder nameProperty = typeBuilder.DefineProperty("name", PropertyAttributes.None, typeof(string), null);
            MethodBuilder nameGetMethod = typeBuilder.DefineMethod("get_name",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName,
                typeof(string), Type.EmptyTypes);
            ILGenerator nameIL = nameGetMethod.GetILGenerator();
            nameIL.Emit(OpCodes.Ldstr, "Theater");
            nameIL.Emit(OpCodes.Ret);
            nameProperty.SetGetMethod(nameGetMethod);

            PropertyBuilder visibleProperty = typeBuilder.DefineProperty("visible", PropertyAttributes.None, typeof(bool), null);
            MethodBuilder visibleGetMethod = typeBuilder.DefineMethod("get_visible",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName,
                typeof(bool), Type.EmptyTypes);

            ILGenerator visibleIL = visibleGetMethod.GetILGenerator();
            visibleIL.Emit(OpCodes.Ldc_I4_1);
            visibleIL.Emit(OpCodes.Ret);
            visibleProperty.SetGetMethod(visibleGetMethod);

            MethodBuilder getValueForMethod = typeBuilder.DefineMethod("GetValueFor",
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(bool),
                [typeof(BeatmapLevel)]);

            ILGenerator getValueForIL = getValueForMethod.GetILGenerator();
            getValueForIL.Emit(OpCodes.Ldarg_1);
            getValueForIL.EmitCall(OpCodes.Call, typeof(VideoLoader).GetMethod("LevelHasVideo", BindingFlags.Public | BindingFlags.Static), null);
            getValueForIL.Emit(OpCodes.Ret);

            MethodBuilder prepareMethod = typeBuilder.DefineMethod("Prepare",
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(Task),
                [typeof(CancellationToken)]);

            ILGenerator prepareMethodIL = prepareMethod.GetILGenerator();
            prepareMethodIL.EmitCall(OpCodes.Call, typeof(Task).GetProperty("CompletedTask", BindingFlags.Public | BindingFlags.Static).GetGetMethod(), null);
            prepareMethodIL.Emit(OpCodes.Ret);

            MethodBuilder contextSwitchMethod = typeBuilder.DefineMethod("ContextSwitch",
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(void),
                [
                    typeof(SelectLevelCategoryViewController.LevelCategory),
                    typeof(BeatmapLevelPack),
                ]);

            var contextSwitchMethodIL = contextSwitchMethod.GetILGenerator();
            contextSwitchMethodIL.Emit(OpCodes.Ret);

            var videoFilterType = typeBuilder.CreateType();
            var instance = Activator.CreateInstance(videoFilterType);

            Type filterMethodsType = betterSongListAssembly.GetType("BetterSongList.FilterMethods");
            MethodInfo registerMethod = filterMethodsType
                .GetMethod("Register")
                .MakeGenericMethod(videoFilterType);

            bool Register()
            {
                var returnValue = registerMethod?.Invoke(null, [instance]);
                if (returnValue != null)
                {
                    return (bool)returnValue;
                }
                else
                {
                    return false;
                }
            }

            return Register;
        }
        catch (Exception e)
        {
            Plugin._log.Warn(e);
            Plugin._log.Warn("Could not register Video Filter");

            return () => false;
        }
    }
}
