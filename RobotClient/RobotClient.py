from typing import Dict
from urllib.parse import urljoin
import requests
import robot_service
from robot_service import get_target_state, get_current_state, put_target_joint


if __name__ == "__main__":
    data, code = get_current_state();
    print(f"current state: {data}")

    data, code = get_target_state();
    print(f"\nTarget state: {data}")

    print("\nupdating target...");
    joints = {
      "r1": 1.0,
      "p2": 2.0,
      "p3": 3.0,
      "p4": 4.0,
      "r5": 5.0
    }
    data, code = put_target_joint(joints);
    print(f"return code: {code}")
    print(f"return data: {data}")
    print("Target updated.")

    data, code = get_target_state();
    print(f"\nTarget state: {data}")

    data, code = get_current_state();
    print(f"\nCurrent state: {data}")




