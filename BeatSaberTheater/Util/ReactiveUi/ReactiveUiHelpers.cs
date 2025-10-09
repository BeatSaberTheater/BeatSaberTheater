using System;
using System.Linq.Expressions;
using System.Reflection;
using Reactive;

namespace BeatSaberTheater.Util.ReactiveUi;

internal class ReactiveUiHelpers
{
	public static void SetupPropertyBinding<T>(ReactiveComponent component, PluginConfig config, Expression<Func<PluginConfig, T>> property,
		Expression<Func<object, object>>? conversion = null)
	{
		component.PropertyChangedEvent += (_, o) =>
		{
			var memberExpression = property.Body as MemberExpression;
			var field = memberExpression?.Member as FieldInfo;
			var prop = memberExpression?.Member as PropertyInfo;

			var valueToWrite = conversion != null ? conversion.Compile()(o) : o;
			if (field != null)
			{
				Plugin._log.Debug("field Value -> " + valueToWrite);
				field.SetValue(config, valueToWrite);
			}

			else if (prop != null)
			{
				Plugin._log.Debug("prop Value -> " + valueToWrite);
				prop.SetValue(config, valueToWrite);
			}
			else
			{
				Plugin._log.Error("Could not find property to write: " + property);
			}
		};
	}
}