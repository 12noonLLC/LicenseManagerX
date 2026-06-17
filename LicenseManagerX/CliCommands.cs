using CommunityToolkit.Diagnostics;
using LicenseManager_12noon.Client;
using System;
using System.Collections.Generic;
using System.IO;

namespace LicenseManagerX;

internal static class CliCommands
{
	private static LicenseManager LoadLicenseOverrideProperties(LicenseOption model)
	{
		// Create license manager
		LicenseManager manager = new();

		// Load private file
		manager.LoadKeypair(model.KeypairPath.FullName);
		Console.WriteLine($"Loaded private information for: {manager.Product} {manager.Version}");

		// Apply CLI overrides
		ApplyOverrides(manager, model);

		// Show overridden properties (if any)
		bool hasOverrides = !string.IsNullOrEmpty(model.ProductVersion)
								|| model.ProductPublishDate.HasValue
								|| (model.ProductFeatures.Count > 0)
								|| model.Type.HasValue || model.Quantity.HasValue
								|| model.ExpirationDays.HasValue
								|| model.ExpirationDate.HasValue
								|| (model.LicenseAttributes.Count > 0)
								|| !string.IsNullOrEmpty(model.LockPath?.FullName);
		if (hasOverrides)
		{
			DisplayOverrideProperties(manager, model);
		}

		return manager;
	}

	internal static void KeypairCreate(LicenseOption model)
	{
		Guard.IsNotNullOrWhiteSpace(model.Passphrase, nameof(model.Passphrase));
		Guard.IsNotNullOrWhiteSpace(model.ProductId, nameof(model.ProductId));
		Guard.IsNotNullOrWhiteSpace(model.ProductName, nameof(model.ProductName));
		Guard.IsNotNullOrWhiteSpace(model.LicenseeName, nameof(model.LicenseeName));
		Guard.IsNotNullOrWhiteSpace(model.LicenseeEmail, nameof(model.LicenseeEmail));

		// Create license manager
		LicenseManager manager = new()
		{
			Passphrase = model.Passphrase,
			ProductId = model.ProductId,
			Product = model.ProductName,
			Name = model.LicenseeName,
			Email = model.LicenseeEmail,
			Company = model.LicenseeCompany ?? string.Empty,

			Version = model.ProductVersion ?? "1.0.0",
			PublishDate = model.ProductPublishDate ?? DateOnly.FromDateTime(MyNow.Now().Date),
			ProductFeatures = new Dictionary<string, string>(model.ProductFeatures),
			StandardOrTrial = model.Type ?? Standard.Licensing.LicenseType.Standard,
			Quantity = model.Quantity ?? 1,
			ExpirationDays = model.ExpirationDays ?? 0,
			ExpirationDate = model.ExpirationDate ?? DateOnly.MaxValue,
			LicenseAttributes = new Dictionary<string, string>(model.LicenseAttributes),
			PathAssembly = model.LockPath?.FullName ?? string.Empty
		};
		manager.IsLockedToAssembly = !string.IsNullOrEmpty(manager.PathAssembly);

		// Create keypair
		manager.CreateKeypair();

		// Save keypair file
		Console.WriteLine();
		Console.WriteLine($"Saving keypair file: {model.KeypairPath.FullName}");
		manager.SaveKeypair(model.KeypairPath.FullName);
		Console.WriteLine("Keypair file saved successfully.");
	}

	internal static void KeypairUpdate(LicenseOption model)
	{
		// Create license manager and load keypair file
		LicenseManager manager = LoadLicenseOverrideProperties(model);

		// Save keypair file
		Console.WriteLine();
		Console.WriteLine($"Saving keypair file: {model.KeypairPath.FullName}");
		manager.SaveKeypair(model.KeypairPath.FullName);
		Console.WriteLine("Keypair file saved successfully.");
	}

	internal static void KeypairShow(FileInfo keypair)
	{
		DisplayKeypairProperties(keypair);
	}

	internal static void LicenseCreate(LicenseOption model)
	{
		// Create license manager and load keypair file
		LicenseManager manager = LoadLicenseOverrideProperties(model);

		// Create license file
		Console.WriteLine();
		Console.WriteLine($"Creating license file: {model.LicensePath!.FullName}");
		manager.SaveLicenseFile(model.LicensePath!.FullName);
		Console.WriteLine("License file created successfully.");
	}

	internal static void LicenseUpdate(LicenseOption model)
	{
		// Create license manager and load keypair file
		LicenseManager manager = LoadLicenseOverrideProperties(model);

		// Update license file
		Console.WriteLine();
		Console.WriteLine($"Updating license file: {model.LicensePath!.FullName}");
		manager.SaveLicenseFile(model.LicensePath!.FullName);
		Console.WriteLine("License file updated successfully.");
	}

	internal static void LicenseShow(FileInfo keypair, FileInfo license)
	{
		DisplayLicenseProperties(keypair, license);
	}

	/// <summary>
	/// Apply CLI overrides to the license manager.
	/// </summary>
	/// <param name="manager">License manager to apply overrides to</param>
	/// <param name="model">License option model containing overrides</param>
	public static void ApplyOverrides(LicenseManager manager, LicenseOption model)
	{
		/// Product Properties
		if (!string.IsNullOrWhiteSpace(model.ProductVersion) && (manager.Version != model.ProductVersion))
		{
			manager.Version = model.ProductVersion;
		}

		if (model.ProductPublishDate.HasValue && (manager.PublishDate != model.ProductPublishDate.Value))
		{
			manager.PublishDate = model.ProductPublishDate.Value;
		}

		// Apply product features if any specified
		if (model.ProductFeatures.Count > 0)
		{
			// Create a new dictionary with existing features plus new ones
			Dictionary<string, string> newFeatures = new(manager.ProductFeatures);
			foreach (var feature in model.ProductFeatures)
			{
				newFeatures[feature.Key] = feature.Value;
			}
			manager.UpdateProductFeatures(newFeatures);
		}

		/// License Properties
		if (model.Type.HasValue && (manager.StandardOrTrial != model.Type.Value))
		{
			manager.StandardOrTrial = model.Type.Value;
		}

		if (model.Quantity.HasValue && (manager.Quantity != model.Quantity.Value))
		{
			manager.Quantity = model.Quantity.Value;
		}

		if (model.ExpirationDays.HasValue && (manager.ExpirationDays != model.ExpirationDays.Value))
		{
			manager.ExpirationDays = model.ExpirationDays.Value;
			// ExpirationDate is automatically updated by the property change handler
		}
		else if (model.ExpirationDate.HasValue && (manager.ExpirationDate != model.ExpirationDate.Value))
		{
			manager.ExpirationDate = model.ExpirationDate.Value;
			manager.ExpirationDays = (int)(model.ExpirationDate.Value.ToDateTime(new()) - DateOnly.FromDateTime(MyNow.Now().Date).ToDateTime(new())).TotalDays;
		}

		// Apply license attributes if any specified
		if (model.LicenseAttributes.Count > 0)
		{
			// Create a new dictionary with existing attributes plus new ones
			Dictionary<string, string> newAttributes = new(manager.LicenseAttributes);
			foreach (var attribute in model.LicenseAttributes)
			{
				newAttributes[attribute.Key] = attribute.Value;
			}
			manager.UpdateLicenseAttributes(newAttributes);
		}

		// Apply lock path if specified
		if (model.LockPath is not null)
		{
			if (manager.PathAssembly != model.LockPath.FullName)
			{
				manager.PathAssembly = model.LockPath.FullName;
				manager.IsLockedToAssembly = true;
			}
		}
	}

	private static void DisplayOverrideProperties(LicenseManager manager, LicenseOption model)
	{
		Console.WriteLine();
		Console.WriteLine("Applied CLI overrides:");
		if (!string.IsNullOrEmpty(model.ProductVersion))
		{
			Console.WriteLine($"  Product Version: {manager.Version}");
		}
		if (model.ProductPublishDate.HasValue)
		{
			Console.WriteLine($"  Product Publish Date: {manager.PublishDate}");
		}
		if (model.ProductFeatures.Count > 0)
		{
			Console.WriteLine($"  Product Features:");
			foreach (var feature in model.ProductFeatures)
			{
				Console.WriteLine($"    {feature.Key} = {feature.Value}");
			}
		}
		if (model.Type.HasValue)
		{
			Console.WriteLine($"  License Type: {manager.StandardOrTrial}");
		}
		if (model.Quantity.HasValue)
		{
			Console.WriteLine($"  Quantity: {manager.Quantity}");
		}
		if (model.ExpirationDays.HasValue)
		{
			Console.WriteLine($"  Expiration Days: {manager.ExpirationDays}");
		}
		if (model.ExpirationDate.HasValue)
		{
			Console.WriteLine($"  Expiration Date: {manager.ExpirationDate:yyyy-MM-dd}");
		}
		if (model.LicenseAttributes.Count > 0)
		{
			Console.WriteLine($"  License Attributes:");
			foreach (var attribute in model.LicenseAttributes)
			{
				Console.WriteLine($"    {attribute.Key} = {attribute.Value}");
			}
		}
		if (model.LockPath is not null)
		{
			Console.WriteLine($"  Lock File: {manager.PathAssembly}");
		}
	}

	private static void DisplayKeypairProperties(FileInfo fileKeypair)
	{
		// Load and display keypair info only
		LicenseManager manager = new();
		manager.LoadKeypair(fileKeypair.FullName);

		Console.WriteLine();

		Console.WriteLine($"Product ID: {manager.ProductId}");
		Console.WriteLine($"Public key: {manager.KeyPublic}");
		Console.WriteLine();

		Console.WriteLine($"Product: {manager.Product}");
		Console.WriteLine($"Version: {manager.Version}");
		if (manager.ProductFeatures.Count > 0)
		{
			Console.WriteLine($"Product features:");
			foreach (var feature in manager.ProductFeatures)
			{
				Console.WriteLine($"  {feature.Key} = {feature.Value}");
			}
		}
		Console.WriteLine();

		Console.WriteLine($"Customer: {manager.Name} <{manager.Email}>");
		if (!string.IsNullOrEmpty(manager.Company))
		{
			Console.WriteLine($"Company: {manager.Company}");
		}
		Console.WriteLine();

		Console.WriteLine($"License type: {manager.StandardOrTrial}");
		Console.WriteLine($"Quantity: {manager.Quantity}");
		if (manager.ExpirationDays > 0)
		{
			Console.WriteLine($"Expiration days: {manager.ExpirationDays} ({((manager.ExpirationDate == DateOnly.MaxValue) ? "None" : manager.ExpirationDate):yyyy-MM-dd})");
		}
		if (manager.LicenseAttributes.Count > 0)
		{
			Console.WriteLine($"License attributes:");
			foreach (var attribute in manager.LicenseAttributes)
			{
				Console.WriteLine($"  {attribute.Key} = {attribute.Value}");
			}
		}
		if (manager.IsLockedToAssembly && !string.IsNullOrEmpty(manager.PathAssembly))
		{
			Console.WriteLine($"Lock file: {manager.PathAssembly} ({(File.Exists(manager.PathAssembly) ? "Exists" : "Does NOT exist")})");
		}
	}

	private static void DisplayLicenseProperties(FileInfo fileKeypair, FileInfo fileLicense)
	{
		// Load and display keypair info only
		LicenseManager manager = new();
		manager.LoadKeypair(fileKeypair.FullName);
		manager.LoadLicenseFile(fileLicense.FullName);

		Console.WriteLine();

		Console.WriteLine($"Product ID: {manager.ProductId}");
		Console.WriteLine($"Public key: {manager.KeyPublic}");
		Console.WriteLine();

		Console.WriteLine($"Product: {manager.Product}");
		Console.WriteLine($"Version: {manager.Version}");
		if (manager.ProductFeatures.Count > 0)
		{
			Console.WriteLine($"Product features:");
			foreach (var feature in manager.ProductFeatures)
			{
				Console.WriteLine($"  {feature.Key} = {feature.Value}");
			}
		}
		Console.WriteLine();

		Console.WriteLine($"Customer: {manager.Name} <{manager.Email}>");
		if (!string.IsNullOrEmpty(manager.Company))
		{
			Console.WriteLine($"Company: {manager.Company}");
		}
		Console.WriteLine();

		Console.WriteLine($"License type: {manager.StandardOrTrial}");
		Console.WriteLine($"Quantity: {manager.Quantity}");
		if (manager.ExpirationDays > 0)
		{
			Console.WriteLine($"Expiration days: {manager.ExpirationDays} ({((manager.ExpirationDate == DateOnly.MaxValue) ? "None" : manager.ExpirationDate):yyyy-MM-dd})");
		}
		if (manager.LicenseAttributes.Count > 0)
		{
			Console.WriteLine($"License attributes:");
			foreach (var attribute in manager.LicenseAttributes)
			{
				Console.WriteLine($"  {attribute.Key} = {attribute.Value}");
			}
		}
		if (manager.IsLockedToAssembly && !string.IsNullOrEmpty(manager.PathAssembly))
		{
			Console.WriteLine($"Lock file: {manager.PathAssembly} ({(File.Exists(manager.PathAssembly) ? "Exists" : "Does NOT exist")})");
		}
	}
}
