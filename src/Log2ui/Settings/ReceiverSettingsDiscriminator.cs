using System;

namespace Log2ui.Settings;

/// <summary>
/// Identifier für Settings De-/Serialization
/// </summary>
/// <param name="Type">Type of <see cref="ReceiverSettings" /> to Deserialize on discriminator <see cref="TypeKey" />.</param>
/// <param name="TypeKey">Discriminator for <see cref="Type" />.</param>
public record ReceiverSettingsDiscriminator(Type Type, string TypeKey);
