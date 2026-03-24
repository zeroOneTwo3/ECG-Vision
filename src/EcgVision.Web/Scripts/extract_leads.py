import sys
import json
import wfdb
import numpy as np

def extract_leads(record_path, mode="4"):
    try:
        # Load the record (signal is a 2D numpy array, fields contains metadata)
        signal, fields = wfdb.rdsamp(record_path)
        
        # PTB-XL Standard Lead Order (0-indexed):
        # I:0, II:1, III:2, aVR:3, aVL:4, aVF:5, V1:6, V2:7, V3:8, V4:9, V5:10, V6:11
        
        if mode == "12":
            # Map all 12 leads
            data = {
                "I": signal[:, 0].tolist(), "II": signal[:, 1].tolist(),
                "III": signal[:, 2].tolist(), "aVR": signal[:, 3].tolist(),
                "aVL": signal[:, 4].tolist(), "aVF": signal[:, 5].tolist(),
                "V1": signal[:, 6].tolist(), "V2": signal[:, 7].tolist(),
                "V3": signal[:, 8].tolist(), "V4": signal[:, 9].tolist(),
                "V5": signal[:, 10].tolist(), "V6": signal[:, 11].tolist()
            }
        else:
            # 4-Lead Screening Mode (Using your specified indices)
            data = {
                "II": signal[:, 1].tolist(),
                "V2": signal[:, 7].tolist(),
                "V4": signal[:, 9].tolist(),
                "V6": signal[:, 11].tolist()
            }
            
        # Include metadata for the worker/frontend
        data["SamplingRate"] = fields.get('fs', 500)
        data["LeadCount"] = 12 if mode == "12" else 4
        
        print(json.dumps(data))

    except Exception as e:
        print(json.dumps({"error": str(e)}), file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Missing record path"}), file=sys.stderr)
        sys.exit(1)

    path = sys.argv[1]
    # Check if a mode was provided (default to 4 if not)
    mode_arg = sys.argv[2] if len(sys.argv) > 2 else "4"
    
    extract_leads(path, mode_arg)