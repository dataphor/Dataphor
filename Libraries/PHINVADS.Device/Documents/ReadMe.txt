This readme provides an example declaration of a PHINVADS device.

create device PHINVADSTestDevice
	reconciliation { mode = { none }, master = device }
	class "PHINVADSDevice"
	attributes
	{
		"Endpoint" = "http://phinvads.cdc.gov/vocabService/v2"
	};
	
Reconcile("FHIRTestDevice");

